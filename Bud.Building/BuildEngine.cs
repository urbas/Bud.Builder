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
    /// <summary>
    ///   The directory where all sources of the build are located.
    /// </summary>
    public string SourceDir { get; }

    /// <summary>
    ///   The directory where all output files should end up.
    /// </summary>
    public string OutputDir { get; }

    /// <summary>
    ///   The directory where this engine will place its build meta files, cached etc.
    /// </summary>
    public string MetaDir { get; }

    /// <summary>
    ///   The directory inside <see cref="MetaDir"/> where finished output directories are located.
    /// </summary>
    public string DoneOutputsDir { get; }

    /// <summary>
    ///   The directory inside <see cref="MetaDir"/> where unfinished output directories are located.
    /// </summary>
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
    public static void Execute(string sourceDir, string outputDir, string metaDir, IEnumerable<IBuildTask> buildTasks)
      => new BuildEngine(sourceDir, outputDir, metaDir).Execute(buildTasks);

    private void Execute(IEnumerable<IBuildTask> buildTasks) {
      CreateMetaOutputDirs();
      ExecuteBuildTasks(buildTasks);
      AssertNoClashingFiles();
      CopyOutputToOutputDir();
    }

    private void CreateMetaOutputDirs() {
      CreateDirectory(DoneOutputsDir);
      if (Exists(PartialOutputsDir)) {
        Delete(PartialOutputsDir, recursive: true);
      }
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
      => new TaskGraph(() => ToTaskGraphAction(buildTask), dependenciesTaskGraphs);

    private void ToTaskGraphAction(IBuildTask buildTask) {
      // At this point all dependencies will have been evaluated.
      var dependenciesResults = buildTask.Dependencies.Select(GetBuildTaskResult).ToImmutableArray();
      var taskSignature = buildTask.Signature(SourceDir, dependenciesResults);
      AssertUniqueSignature(buildTask, taskSignature);

      var taskOutputDir = Combine(DoneOutputsDir, taskSignature);
      if (!Exists(taskOutputDir)) {
        var partialTaskOutputDir = Combine(PartialOutputsDir, taskSignature);
        ExecuteBuildTask(buildTask, partialTaskOutputDir, taskOutputDir, SourceDir,
                         dependenciesResults);
      }

      var buildTaskResult = new BuildTaskResult(taskSignature, taskOutputDir, dependenciesResults);
      buildTasksToResults.TryAdd(buildTask, buildTaskResult);
    }

    private BuildTaskResult GetBuildTaskResult(IBuildTask task) => buildTasksToResults[task];

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
}