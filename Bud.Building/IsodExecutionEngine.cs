using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using static System.IO.Directory;
using static System.IO.Path;
using static Bud.FileUtils;

namespace Bud {
  /// <summary>
  ///   The name of this class stands for Isolated Signed Output Directories Execution Engine.
  ///
  ///   This execution engine creates an output directory for each task.
  /// </summary>
  /// <remarks>
  ///   Assuming that task A has not yet been executed. Here's how this execution engine will execute task A:
  ///
  ///   <ul>
  ///     <li>Retrieve the signature of task A.</li>
  ///
  ///     <li>There is no output directory that corresponds to the signature.</li>
  ///
  ///     <li>Create a directory where the task should place its output files.</li>
  ///
  ///     <li>Create a build context for the task. The build context contains the path of the output directory where the
  ///       task should place its files, a list of output directories of the task's dependencies, and some other
  ///       ancillary information.</li>
  ///
  ///     <li>Execute the task and wait for it to finish.</li>
  ///   </ul>
  ///
  ///
  ///   Assuming that task A was already executed. Here's how this exection engine will execute task A:
  ///
  ///   <ul>
  ///     <li>Retrieve the signature of task A.</li>
  ///
  ///     <li>Find the task output directory that corresponds to the signature.</li>
  ///
  ///     <li>The directory is present.</li>
  ///
  ///     <li>Do not execute the task.</li>
  ///   </ul>
  /// </remarks>
  public class IsodExecutionEngine {
    ///  <summary>
    ///  </summary>
    /// <param name="sourceDir">the directory in which all the sources relevant to the build reside.</param>
    /// <param name="buildDir">
    ///   the directory in which all build output will be placed (including build metadata).
    /// </param>
    /// <param name="metaDir">the directory in which the execution engine will store temporary build artifacts and
    /// build metadata.</param>
    /// <param name="buildTasks">the build tasks to execute.</param>
    /// <returns>an object containing information about the resulting build.</returns>
    ///  <exception cref="Exception">this exception is thrown if the build fails for any reason.</exception>
    public static void Execute(string sourceDir, string buildDir, string metaDir, params IBuildTask[] buildTasks)
      => Execute(sourceDir, buildDir, metaDir, buildTasks as IEnumerable<IBuildTask>);

    ///  <summary>
    ///  </summary>
    ///  <param name="sourceDir">the directory in which all the sources relevant to the build reside.</param>
    ///  <param name="buildDir">
    ///    the directory in which all build output will be placed (including build metadata).
    ///  </param>
    /// <param name="metaDir">the directory in which the execution engine will store temporary build artifacts and
    /// build metadata.</param>
    /// <param name="buildTasks">the build tasks to execute.</param>
    ///  <returns>an object containing information about the resulting build.</returns>
    ///  <exception cref="Exception">this exception is thrown if the build fails for any reason.</exception>
    public static void Execute(string sourceDir, string buildDir, string metaDir, IEnumerable<IBuildTask> buildTasks) {
      var buildExecutionContext = new BuildExecutionContext(sourceDir, buildDir, metaDir);
      try {
        new TaskGraph(buildTasks.Select(buildTask => GetOrCreateTaskGraph(buildExecutionContext, buildTask))).Run();
      } catch (AggregateException aggregateException) {
        throw aggregateException.InnerExceptions[0];
      }

      if (Exists(buildDir)) {
        Delete(buildDir, true);
      }
      foreach (var taskOutputDir in buildExecutionContext.TaskOutputDirs) {
        CopyTree(taskOutputDir, buildDir);
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

    private static TaskGraph CreateTaskGraph(BuildExecutionContext buildExecutionContext, IBuildTask buildTask) {
      var dependenciesTaskGraphs = buildTask.Dependencies
                                            .Select(depTask => GetOrCreateTaskGraph(buildExecutionContext, depTask))
                                            .ToImmutableArray();
      return ToTaskGraph(buildExecutionContext, buildTask, dependenciesTaskGraphs);
    }

    private static TaskGraph ToTaskGraph(BuildExecutionContext buildExecutionContext, IBuildTask buildTask,
                                         ImmutableArray<TaskGraph> dependenciesTaskGraphs)
      => new TaskGraph(() => {
        var dependenciesResults = buildTask.Dependencies
                                           .Select(buildExecutionContext.GetBuildTaskResult)
                                           .ToImmutableArray();

        var taskSignature = buildTask.GetSignature(dependenciesResults);

        AssertUniqueSignature(buildExecutionContext, buildTask, taskSignature);

        var taskOutputDir = Combine(buildExecutionContext.DoneOutputsDir, taskSignature);
        if (!Exists(taskOutputDir)) {
          var partialTaskOutputDir = Combine(buildExecutionContext.PartialOutputsDir, taskSignature);
          ExecuteBuildTask(buildTask, partialTaskOutputDir, taskOutputDir, buildExecutionContext.SourceDir);
        }
        CollectBuildTaskOutput(buildExecutionContext, buildTask, taskOutputDir);

        var buildTaskResult = new BuildTaskResult(buildTask, taskSignature, taskOutputDir,
                                                  buildExecutionContext.GetAbsoluteOutputs(buildTask),
                                                  dependenciesResults);


        buildExecutionContext.AddBuildTaskResult(buildTask, buildTaskResult);
      }, dependenciesTaskGraphs);

    private static void ExecuteBuildTask(IBuildTask buildTask, string partialTaskOutputDir, string taskOutputDir,
                                         string sourceDir) {
      CreateDirectory(partialTaskOutputDir);
      buildTask.Execute(new BuildTaskContext(partialTaskOutputDir, sourceDir));
      Move(partialTaskOutputDir, taskOutputDir);
    }

    private static void CollectBuildTaskOutput(BuildExecutionContext outputFilesToTasks, IBuildTask buildTask,
                                               string taskOutputDir) {
      var relativeOutputFilesEnumerable = FindFilesRelative(taskOutputDir);
      var relativeOutputFilePaths = relativeOutputFilesEnumerable as IList<string> ?? relativeOutputFilesEnumerable.ToList();
      foreach (var outputFile in relativeOutputFilePaths) {
        IBuildTask otherTask;
        if (outputFilesToTasks.TryGetBuildTaskForRelativeFile(outputFile, out otherTask)) {
          throw new Exception($"Tasks '{otherTask.Name}' and '{buildTask.Name}' are clashing. " +
                              $"They produced the same file '{outputFile}'.");
        }
      }
      outputFilesToTasks.AddOutputFiles(taskOutputDir, relativeOutputFilePaths, buildTask);
    }

    private static void AssertUniqueSignature(BuildExecutionContext buildExecutionContext, IBuildTask buildTask, string taskSignature) {
      var storedTask = buildExecutionContext.GetOrAddTaskSignature(taskSignature, buildTask);
      if (storedTask != buildTask) {
        throw new Exception($"Tasks '{storedTask.Name}' and '{buildTask.Name}' are clashing. " +
                            $"They have the same signature '{taskSignature}'.");
      }
    }

    private class BuildExecutionContext {
      private readonly Dictionary<string, IBuildTask> relativeOutputFilesToBuildTasks = new Dictionary<string, IBuildTask>();
      private readonly List<string> outputFilesAbsPaths = new List<string>();

      private readonly Dictionary<IBuildTask, ImmutableArray<string>> buildTasksToAbsoluteOutputFiles
        = new Dictionary<IBuildTask, ImmutableArray<string>>();

      private readonly Dictionary<IBuildTask, BuildTaskResult> buildTasksToResults
        = new Dictionary<IBuildTask, BuildTaskResult>();

      private readonly Dictionary<IBuildTask, TaskGraph> buildTaskToTaskGraph = new Dictionary<IBuildTask, TaskGraph>();

      private readonly ConcurrentDictionary<string, IBuildTask> signatureToBuildTask
        = new ConcurrentDictionary<string, IBuildTask>();

      private List<string> taskOutputDirs = new List<string>();


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

      public string MetaDir { get; }

      public string DoneOutputsDir { get; }

      public string PartialOutputsDir { get; }

      public ImmutableArray<string> OutputFiles => outputFilesAbsPaths.ToImmutableArray();
      public IEnumerable<string> TaskOutputDirs => taskOutputDirs;

      public bool TryGetBuildTaskForRelativeFile(string relativeOutputFile, out IBuildTask buildTask)
        => relativeOutputFilesToBuildTasks.TryGetValue(relativeOutputFile, out buildTask);

      public void AddOutputFiles(string taskOutputDir, IList<string> relativeOutputFilePaths, IBuildTask buildTask) {
        foreach (var relativeOutputFilePath in relativeOutputFilePaths) {
          relativeOutputFilesToBuildTasks.Add(relativeOutputFilePath, buildTask);
        }
        var absoluteOutputFilePaths = relativeOutputFilePaths
          .Select(relativeOutputFile => Combine(taskOutputDir, relativeOutputFile))
          .ToImmutableArray();
        outputFilesAbsPaths.AddRange(absoluteOutputFilePaths);
        buildTasksToAbsoluteOutputFiles.Add(buildTask, absoluteOutputFilePaths);
        taskOutputDirs.Add(taskOutputDir);
      }

      public ImmutableArray<string> GetAbsoluteOutputs(IBuildTask buildTask)
        => buildTasksToAbsoluteOutputFiles[buildTask];

      public void AddBuildTaskResult(IBuildTask buildTask, BuildTaskResult buildTaskResult)
        => buildTasksToResults.Add(buildTask, buildTaskResult);

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
    void Execute(BuildTaskContext context);
    ImmutableArray<IBuildTask> Dependencies { get; }
    string Name { get; }

    /// <param name="dependencyResults">the results of dependent build tasks.</param>
    /// <returns>
    ///   A hex string or a URL- and filename-safe Base64 string (i.e.: base64url). This signature should be a
    ///   cryptographically strong digest of the tasks inputs such as source files, signatures of dependncies,
    ///   environment variables, the task's algorithm, and other factors that affect the task's output.
    /// </returns>
    string GetSignature(ImmutableArray<BuildTaskResult> dependencyResults);
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
    public ImmutableArray<string> OutputFiles { get; }
    public ImmutableArray<BuildTaskResult> DependenciesResults { get; }

    public BuildTaskResult(IBuildTask buildTask, string taskSignature, string taskOutputDir,
                           ImmutableArray<string> outputFiles, ImmutableArray<BuildTaskResult> dependenciesResults) {
      TaskSignature = taskSignature;
      TaskOutputDir = taskOutputDir;
      OutputFiles = outputFiles;
      DependenciesResults = dependenciesResults;
      this.buildTask = buildTask;
    }
  }
}