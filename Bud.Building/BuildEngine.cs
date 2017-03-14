using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static System.IO.Directory;
using static System.IO.Path;
using static Bud.FileUtils;

namespace Bud {
  /// <summary>
  ///   This build engine tries to isolate tasks by creating a signed output directory for each task.
  /// </summary>
  /// <remarks>
  ///   Here's how this engine processes a build task:
  ///
  ///   <ul>
  ///     <li>Execute all direct dependencies of task A.</li>
  ///
  ///     <li>Retrieve the signature of task A.</li>
  ///
  ///     <li>Check that no other task has the same signature.</li>
  ///
  ///     <li>Check if an output directory with same signature already exists.</li>
  ///
  ///     <li>If the directory exists, go to the last step, if not, continue with the next step.</li>
  ///
  ///     <li>Create a temporary directory and ask the task to place its output into this directory.</li>
  ///
  ///     <li>Abort the build if the task threw an exception. If not, rename the temporary directory to the
  ///     signature of the task.</li>
  ///
  ///     <li>Store the output directory in a list.</li>
  ///   </ul>
  ///
  ///   The end result is a list of all output directories. The engine now copies the contents of all output directories
  ///   into the output directory.
  /// </remarks>
  public class BuildEngine {
    public string SourceDir { get; }
    public string OutputDir { get; }
    public string MetaDir { get; }
    public string DoneOutputsDir { get; }
    public string PartialOutputsDir { get; }

    /// <summary>
    ///   Task graphs are built up in a single thread. This is why this can be a normal dictionary.
    /// </summary>
    private readonly Dictionary<IBuildTask, TaskGraph> buildTaskToTaskGraph = new Dictionary<IBuildTask, TaskGraph>();

    private readonly ConcurrentDictionary<IBuildTask, BuildTaskResult> buildTasksToResults
      = new ConcurrentDictionary<IBuildTask, BuildTaskResult>(new Dictionary<IBuildTask, BuildTaskResult>());

    private readonly ConcurrentDictionary<string, IBuildTask> signatureToBuildTask
      = new ConcurrentDictionary<string, IBuildTask>();

    private BuildEngine(string sourceDir, string outputDir, string metaDir) {
      SourceDir = sourceDir;
      OutputDir = outputDir;
      MetaDir = metaDir;
      PartialOutputsDir = Combine(MetaDir, ".partial");
      DoneOutputsDir = Combine(MetaDir, ".done");
    }

    ///  <summary>
    ///  </summary>
    /// <param name="sourceDir">the directory in which all the sources relevant to the build reside.</param>
    /// <param name="outputDir">
    ///   the directory in which all build output will be placed (including build metadata).
    /// </param>
    /// <param name="metaDir">the directory in which the execution engine will store temporary build artifacts and
    /// build metadata.</param>
    /// <param name="buildTasks">the build tasks to execute.</param>
    /// <returns>an object containing information about the resulting build.</returns>
    ///  <exception cref="Exception">this exception is thrown if the build fails for any reason.</exception>
    public static void Execute(string sourceDir, string outputDir, string metaDir, params IBuildTask[] buildTasks)
      => Execute(sourceDir, outputDir, metaDir, buildTasks as IEnumerable<IBuildTask>);

    ///  <summary>
    ///  </summary>
    ///  <param name="sourceDir">the directory in which all the sources relevant to the build reside.</param>
    ///  <param name="outputDir">
    ///    the directory in which all build output will be placed (including build metadata).
    ///  </param>
    /// <param name="metaDir">the directory in which the execution engine will store temporary build artifacts and
    /// build metadata.</param>
    /// <param name="buildTasks">the build tasks to execute.</param>
    ///  <returns>an object containing information about the resulting build.</returns>
    ///  <exception cref="Exception">this exception is thrown if the build fails for any reason.</exception>
    public static void Execute(string sourceDir, string outputDir, string metaDir, IEnumerable<IBuildTask> buildTasks) {
      new BuildEngine(sourceDir, outputDir, metaDir).Execute(buildTasks);
    }

    private void Execute(IEnumerable<IBuildTask> buildTasks) {
      CreateMetaOutputDirs();
      ExecuteBuildTasks(buildTasks);
      AssertNoClashingFiles();
      CopyOutputToOutputDir();
    }

    private void CreateMetaOutputDirs() {
      CreateDirectory(DoneOutputsDir);
      CreateDirectory(PartialOutputsDir);
    }

    private void ExecuteBuildTasks(IEnumerable<IBuildTask> buildTasks) {
      try {
        new TaskGraph(buildTasks.Select(GetOrCreateTaskGraph)).Run();
      } catch (AggregateException aggregateException) {
        throw aggregateException.InnerExceptions[0];
      }
    }

    private void AssertNoClashingFiles() {
      var relativeOutputFileToBuildTask = new Dictionary<string, IBuildTask>();
      foreach (var signatureAndBuildTask in signatureToBuildTask) {
        var relativeOutputFiles = FindFilesRelative(Combine(DoneOutputsDir, signatureAndBuildTask.Key));

        foreach (var relativeOutputFile in relativeOutputFiles) {
          IBuildTask otherTask;

          if (relativeOutputFileToBuildTask.TryGetValue(relativeOutputFile, out otherTask)) {
            throw new Exception($"Tasks '{otherTask.Name}' and '{signatureAndBuildTask.Value.Name}' are clashing. " +
                                $"They produced the same file '{relativeOutputFile}'.");
          }
          relativeOutputFileToBuildTask.Add(relativeOutputFile, signatureAndBuildTask.Value);
        }
      }
    }

    private void CopyOutputToOutputDir() {
      if (Exists(OutputDir)) {
        Delete(OutputDir, true);
      }
      foreach (var taskOutputDir in TaskOutputDirs) {
        CopyTree(taskOutputDir, OutputDir);
      }
    }

    private TaskGraph GetOrCreateTaskGraph(IBuildTask buildTask) {
      TaskGraph taskGraph;
      if (buildTaskToTaskGraph.TryGetValue(buildTask, out taskGraph)) {
        return taskGraph;
      }
      var createdTaskGraph = CreateTaskGraph(buildTask);
      buildTaskToTaskGraph.Add(buildTask, createdTaskGraph);
      return createdTaskGraph;
    }

    private IEnumerable<string> TaskOutputDirs
      => signatureToBuildTask.Keys.Select(sig => Combine(DoneOutputsDir, sig));

    private TaskGraph CreateTaskGraph(IBuildTask buildTask)
      => ToTaskGraph(buildTask, buildTask.Dependencies.Select(GetOrCreateTaskGraph).ToImmutableArray());

    private TaskGraph ToTaskGraph(IBuildTask buildTask, ImmutableArray<TaskGraph> dependenciesTaskGraphs)
      => new TaskGraph(() => {
        // At this point all dependencies will have been evaluated, so their results will be available.
        var dependenciesResults = buildTask.Dependencies
                                           .Select(task => buildTasksToResults[task])
                                           .ToImmutableArray();

        var taskSignature = buildTask.GetSignature(SourceDir, dependenciesResults);

        AssertUniqueSignature(buildTask, taskSignature);

        var taskOutputDir = Combine(DoneOutputsDir, taskSignature);
        if (!Exists(taskOutputDir)) {
          var partialTaskOutputDir = Combine(PartialOutputsDir, taskSignature);
          ExecuteBuildTask(buildTask, partialTaskOutputDir, taskOutputDir, SourceDir,
          dependenciesResults);
        }

        var buildTaskResult = new BuildTaskResult(buildTask, taskSignature, taskOutputDir, dependenciesResults);

        buildTasksToResults.TryAdd(buildTask, buildTaskResult);
      }, dependenciesTaskGraphs);

    private static void ExecuteBuildTask(IBuildTask buildTask, string partialTaskOutputDir, string taskOutputDir,
                                         string sourceDir, ImmutableArray<BuildTaskResult> dependenciesResults) {
      CreateDirectory(partialTaskOutputDir);
      buildTask.Execute(new BuildTaskContext(partialTaskOutputDir, sourceDir), dependenciesResults);
      Move(partialTaskOutputDir, taskOutputDir);
    }

    private void AssertUniqueSignature(IBuildTask buildTask, string taskSignature) {
      var storedTask = signatureToBuildTask.GetOrAdd(taskSignature, buildTask);
      if (storedTask != buildTask) {
        throw new Exception($"Tasks '{storedTask.Name}' and '{buildTask.Name}' are clashing. " +
                            $"They have the same signature '{taskSignature}'.");
      }
    }
  }

  public interface IBuildTask {
    void Execute(BuildTaskContext context, ImmutableArray<BuildTaskResult> dependencyResults);
    ImmutableArray<IBuildTask> Dependencies { get; }
    string Name { get; }

    /// <param name="ctx">this object contains the source directory of the current build, the output directory where
    ///   this task should place its files, and other information about the current build.</param>
    /// <param name="dependencyResults">the results of dependent build tasks.</param>
    /// <returns>
    ///   A hex string or a URL- and filename-safe Base64 string (i.e.: base64url). This signature should be a
    ///   cryptographically strong digest of the tasks inputs such as source files, signatures of dependncies,
    ///   environment variables, the task's algorithm, and other factors that affect the task's output.
    /// </returns>
    string GetSignature(string ctx, ImmutableArray<BuildTaskResult> dependencyResults);
  }

  public class BuildTaskContext {
    public string OutputDir { get; }
    public string SourceDir { get; }

    public BuildTaskContext(string outputDir, string sourceDir) {
      OutputDir = outputDir;
      SourceDir = sourceDir;
    }
  }

  public class BuildTaskResult {
    private readonly IBuildTask buildTask;
    public string TaskName => buildTask.Name;
    public string TaskSignature { get; }
    public string TaskOutputDir { get; }
    public ImmutableArray<BuildTaskResult> DependenciesResults { get; }

    public BuildTaskResult(IBuildTask buildTask, string taskSignature, string taskOutputDir,
                           ImmutableArray<BuildTaskResult> dependenciesResults) {
      TaskSignature = taskSignature;
      TaskOutputDir = taskOutputDir;
      DependenciesResults = dependenciesResults;
      this.buildTask = buildTask;
    }
  }
}