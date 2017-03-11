using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// <summary>
    ///
    /// </summary>
    /// <param name="sourceDir">the directory in which all the sources relevant to the build reside.</param>
    /// <param name="buildDir">
    ///   the directory in which all build output will be placed (including build metadata).
    /// </param>
    /// <param name="buildTasks">the build tasks to execute.</param>
    /// <returns>an object containing information about the resulting build.</returns>
    /// <exception cref="Exception">this exception is thrown if the build fails for any reason.</exception>
    public static EntireBuildResult Execute(string sourceDir, string buildDir, params IBuildTask[] buildTasks)
      => Execute(sourceDir, buildDir, buildTasks as IEnumerable<IBuildTask>);

    /// <summary>
    ///
    /// </summary>
    /// <param name="sourceDir">the directory in which all the sources relevant to the build reside.</param>
    /// <param name="buildDir">
    ///   the directory in which all build output will be placed (including build metadata).
    /// </param>
    /// <param name="buildTasks">the build tasks to execute.</param>
    /// <returns>an object containing information about the resulting build.</returns>
    /// <exception cref="Exception">this exception is thrown if the build fails for any reason.</exception>
    public static EntireBuildResult Execute(string sourceDir, string buildDir, IEnumerable<IBuildTask> buildTasks) {
      var buildExecutionContext = new BuildExecutionContext(sourceDir, buildDir);
      ExecuteImpl(buildExecutionContext, buildTasks as ICollection<IBuildTask> ?? buildTasks.ToList());
      return new EntireBuildResult(buildExecutionContext.OutputFiles);
    }

    private static ImmutableArray<BuildTaskResult> ExecuteImpl(BuildExecutionContext buildExecutionContext,
                                                               ICollection<IBuildTask> buildTasks) {
      var unfinishedOutputDir = Combine(buildExecutionContext.BuildDir, ".unfinished_output");
      var outputCache = Combine(buildExecutionContext.BuildDir, ".output_cache");
      CreateDirectory(outputCache);
      CreateDirectory(unfinishedOutputDir);

      var dependenciesResultsBuilder = ImmutableArray.CreateBuilder<BuildTaskResult>(buildTasks.Count);

      foreach (var buildTask in buildTasks) {
        var buildTaskResults = ExecuteImpl(buildExecutionContext, buildTask.Dependencies);
        var taskSignature = buildTask.GetSignature(buildTaskResults);
        var taskOutputDir = Combine(outputCache, taskSignature);
        if (!Exists(taskOutputDir)) {
          var unfinishedTaskOutputDir = Combine(unfinishedOutputDir, taskSignature);
          ExecuteBuildTask(buildTask, unfinishedTaskOutputDir, taskOutputDir);
        }
        CollectBuildTaskOutput(buildExecutionContext, buildTask, taskOutputDir);

        dependenciesResultsBuilder.Add(new BuildTaskResult(buildTask, taskSignature, taskOutputDir,
                                                           buildExecutionContext.GetAbsoluteOutputs(buildTask),
                                       buildTaskResults));
      }

      return dependenciesResultsBuilder.MoveToImmutable();
    }

    private static void ExecuteBuildTask(IBuildTask buildTask, string unfinishedTaskOutputDir, string taskOutputDir) {
      CreateDirectory(unfinishedTaskOutputDir);
      buildTask.Execute(unfinishedTaskOutputDir);
      Move(unfinishedTaskOutputDir, taskOutputDir);
    }

    private static void CollectBuildTaskOutput(BuildExecutionContext outputFilesToTasks, IBuildTask buildTask,
                                               string taskOutputDir) {
      var relativeOutputFilesEnumerable = FindFilesRelative(taskOutputDir);
      var relativeOutputFilePaths = relativeOutputFilesEnumerable as IList<string> ?? relativeOutputFilesEnumerable.ToList();
      foreach (var outputFile in relativeOutputFilePaths) {
        IBuildTask otherTask;
        if (outputFilesToTasks.TryGetBuildTask(outputFile, out otherTask)) {
          throw new Exception($"Tasks '{otherTask.Name}' and '{buildTask.Name}' are clashing. " +
                              $"They produced the same file '{outputFile}'.");
        }
      }
      outputFilesToTasks.AddOutputFiles(taskOutputDir, relativeOutputFilePaths, buildTask);
    }

    private class BuildExecutionContext {
      public string SourceDir { get; }
      public string BuildDir { get; }
      private readonly Dictionary<string, IBuildTask> outputFilesToBuildTasks = new Dictionary<string, IBuildTask>();
      private readonly List<string> outputFilesAbsPaths = new List<string>();
      private readonly Dictionary<IBuildTask, ImmutableArray<string>> buildTasksToAbsoluteOutputFiles
        = new Dictionary<IBuildTask, ImmutableArray<string>>();

      public BuildExecutionContext(string sourceDir, string buildDir) {
        SourceDir = sourceDir;
        BuildDir = buildDir;
      }

      public ImmutableArray<string> OutputFiles => outputFilesAbsPaths.ToImmutableArray();

      public bool TryGetBuildTask(string outputFile, out IBuildTask buildTask)
        => outputFilesToBuildTasks.TryGetValue(outputFile, out buildTask);

      public void AddOutputFiles(string taskOutputDir, IList<string> relativeOutputFilePaths, IBuildTask buildTask) {
        foreach (var relativeOutputFilePath in relativeOutputFilePaths) {
          outputFilesToBuildTasks.Add(relativeOutputFilePath, buildTask);
        }
        var absoluteOutputFilePaths = relativeOutputFilePaths
          .Select(relativeOutputFile => Combine(taskOutputDir, relativeOutputFile))
          .ToImmutableArray();
        outputFilesAbsPaths.AddRange(absoluteOutputFilePaths);
        buildTasksToAbsoluteOutputFiles.Add(buildTask, absoluteOutputFilePaths);
      }

      public ImmutableArray<string> GetAbsoluteOutputs(IBuildTask buildTask)
        => buildTasksToAbsoluteOutputFiles[buildTask];
    }
  }

  public interface IBuildTask {
    void Execute(string buildDir);
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

  public class BuildTaskResult {
    public string TaskSignature { get; }
    public string TaskOutputDir { get; }
    public ImmutableArray<string> OutputFiles { get; }
    public ImmutableArray<BuildTaskResult> DependenciesResults { get; }
    private readonly IBuildTask buildTask;

    public BuildTaskResult(IBuildTask buildTask, string taskSignature, string taskOutputDir,
                           ImmutableArray<string> outputFiles, ImmutableArray<BuildTaskResult> dependenciesResults) {
      TaskSignature = taskSignature;
      TaskOutputDir = taskOutputDir;
      OutputFiles = outputFiles;
      DependenciesResults = dependenciesResults;
      this.buildTask = buildTask;
    }

    public string TaskName => buildTask.Name;
  }

  /// <summary>
  ///   This class contains a map of build tasks and their output directories.
  /// </summary>
  public class EntireBuildResult {
    /// <summary>
    ///   Initializes the build result object.
    /// </summary>
    /// <param name="outputFiles">see <see cref="OutputFiles"/>.</param>
    public EntireBuildResult(ImmutableArray<string> outputFiles) {
      OutputFiles = outputFiles;
    }

    /// <summary>
    ///   The list of all output files produced by the build.
    /// </summary>
    public ImmutableArray<string> OutputFiles { get; }
  }
}