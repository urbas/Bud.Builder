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
      var buildExecutionContext = new BuildExecutionContext(sourceDir, outputDir, metaDir);
      ExecuteBuildTasks(buildTasks, buildExecutionContext);
      AssertNoClashingFiles(buildExecutionContext);
      CopyOutputToOutputDir(buildExecutionContext);
    }

    private static void ExecuteBuildTasks(IEnumerable<IBuildTask> buildTasks, BuildExecutionContext buildExecutionContext) {
      try {
        new TaskGraph(buildTasks.Select(buildTask => GetOrCreateTaskGraph(buildExecutionContext, buildTask))).Run();
      } catch (AggregateException aggregateException) {
        throw aggregateException.InnerExceptions[0];
      }
    }

    private static void CopyOutputToOutputDir(BuildExecutionContext buildExecutionContext) {
      if (Exists(buildExecutionContext.BuildDir)) {
        Delete(buildExecutionContext.BuildDir, true);
      }
      foreach (var taskOutputDir in buildExecutionContext.TaskOutputDirs) {
        CopyTree(taskOutputDir, buildExecutionContext.BuildDir);
      }
    }

    private static TaskGraph GetOrCreateTaskGraph(BuildExecutionContext buildExecutionContext, IBuildTask buildTask) {
      TaskGraph taskGraph;
      if (buildExecutionContext.TryGetTaskGraph(buildTask, out taskGraph)) {
        return taskGraph;
      }
      var createdTaskGraph = CreateTaskGraph(buildExecutionContext, buildTask);
      buildExecutionContext.AddTaskGraph(buildTask, createdTaskGraph);
      return createdTaskGraph;
    }

    private static TaskGraph CreateTaskGraph(BuildExecutionContext buildExecutionContext, IBuildTask buildTask)
      => ToTaskGraph(buildExecutionContext,
                     buildTask,
                     buildTask.Dependencies
                              .Select(depTask => GetOrCreateTaskGraph(buildExecutionContext, depTask))
                              .ToImmutableArray());

    private static TaskGraph ToTaskGraph(BuildExecutionContext buildExecutionContext, IBuildTask buildTask,
                                         ImmutableArray<TaskGraph> dependenciesTaskGraphs)
      => new TaskGraph(() => {
        // At this point all dependencies will have been evaluated, so their results will be available.
        var dependenciesResults = buildTask.Dependencies
                                           .Select(buildExecutionContext.GetBuildTaskResult)
                                           .ToImmutableArray();

        var taskSignature = buildTask.GetSignature(buildExecutionContext.SourceDir, dependenciesResults);

        AssertUniqueSignature(buildExecutionContext, buildTask, taskSignature);

        var taskOutputDir = Combine(buildExecutionContext.DoneOutputsDir, taskSignature);
        if (!Exists(taskOutputDir)) {
          var partialTaskOutputDir = Combine(buildExecutionContext.PartialOutputsDir, taskSignature);
          ExecuteBuildTask(buildTask, partialTaskOutputDir, taskOutputDir, buildExecutionContext.SourceDir,
          dependenciesResults);
        }

        var buildTaskResult = new BuildTaskResult(buildTask, taskSignature, taskOutputDir, dependenciesResults);

        buildExecutionContext.AddBuildTaskResult(buildTask, buildTaskResult);
      }, dependenciesTaskGraphs);

    private static void ExecuteBuildTask(IBuildTask buildTask, string partialTaskOutputDir, string taskOutputDir,
                                         string sourceDir, ImmutableArray<BuildTaskResult> dependenciesResults) {
      CreateDirectory(partialTaskOutputDir);
      buildTask.Execute(new BuildTaskContext(partialTaskOutputDir, sourceDir), dependenciesResults);
      Move(partialTaskOutputDir, taskOutputDir);
    }

    private static void AssertNoClashingFiles(BuildExecutionContext buildExecutionContext) {
      var relativeOutputFileToBuildTask = new Dictionary<string, IBuildTask>();
      foreach (var signatureAndBuildTask in buildExecutionContext.SignaturesToBuildTasks) {
        var relativeOutputFilesEnumerable =
          FindFilesRelative(Combine(buildExecutionContext.DoneOutputsDir, signatureAndBuildTask.Key));

        foreach (var relativeOutputFile in relativeOutputFilesEnumerable) {
          IBuildTask otherTask;

          if (relativeOutputFileToBuildTask.TryGetValue(relativeOutputFile, out otherTask)) {
            throw new Exception($"Tasks '{otherTask.Name}' and '{signatureAndBuildTask.Value.Name}' are clashing. " +
                                $"They produced the same file '{relativeOutputFile}'.");
          }
          relativeOutputFileToBuildTask.Add(relativeOutputFile, signatureAndBuildTask.Value);
        }
      }
    }

    private static void AssertUniqueSignature(BuildExecutionContext buildExecutionContext, IBuildTask buildTask, string taskSignature) {
      var storedTask = buildExecutionContext.GetOrAddTaskSignature(taskSignature, buildTask);
      if (storedTask != buildTask) {
        throw new Exception($"Tasks '{storedTask.Name}' and '{buildTask.Name}' are clashing. " +
                            $"They have the same signature '{taskSignature}'.");
      }
    }

    private class BuildExecutionContext {
      /// <summary>
      ///   Task graphs are built up in a single thread. This is why this can be a normal dictionary.
      /// </summary>
      private readonly Dictionary<IBuildTask, TaskGraph> buildTaskToTaskGraph = new Dictionary<IBuildTask, TaskGraph>();

      private readonly ConcurrentDictionary<IBuildTask, BuildTaskResult> buildTasksToResults
        = new ConcurrentDictionary<IBuildTask, BuildTaskResult>(new Dictionary<IBuildTask, BuildTaskResult>());

      private readonly ConcurrentDictionary<string, IBuildTask> signatureToBuildTask
        = new ConcurrentDictionary<string, IBuildTask>();

      public BuildExecutionContext(string sourceDir, string buildDir, string metaDir) {
        SourceDir = sourceDir;
        BuildDir = buildDir;
        MetaDir = metaDir;
        PartialOutputsDir = Combine(MetaDir, ".partial");
        DoneOutputsDir = Combine(MetaDir, ".done");

        CreateDirectory(DoneOutputsDir);
        CreateDirectory(PartialOutputsDir);
      }

      public string SourceDir { get; }

      public string BuildDir { get; }

      private string MetaDir { get; }

      public string DoneOutputsDir { get; }

      public string PartialOutputsDir { get; }

      public IEnumerable<string> TaskOutputDirs
        => signatureToBuildTask.Keys.Select(sig => Combine(DoneOutputsDir, sig));

      public IDictionary<string, IBuildTask> SignaturesToBuildTasks => signatureToBuildTask;

      public void AddBuildTaskResult(IBuildTask buildTask, BuildTaskResult buildTaskResult)
        => buildTasksToResults.GetOrAdd(buildTask, buildTaskResult);

      public BuildTaskResult GetBuildTaskResult(IBuildTask buildTask) => buildTasksToResults[buildTask];

      public bool TryGetTaskGraph(IBuildTask buildTask, out TaskGraph taskGraph)
        => buildTaskToTaskGraph.TryGetValue(buildTask, out taskGraph);

      public void AddTaskGraph(IBuildTask buildTask, TaskGraph taskGraph)
        => buildTaskToTaskGraph.Add(buildTask, taskGraph);

      public IBuildTask GetOrAddTaskSignature(string taskSignature, IBuildTask buildTask)
        => signatureToBuildTask.GetOrAdd(taskSignature, buildTask);
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