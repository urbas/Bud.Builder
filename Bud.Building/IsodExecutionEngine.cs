using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static System.IO.Directory;
using static System.IO.Path;
using static System.IO.SearchOption;

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
    public static EntireBuildResult Execute(string sourceDir, string buildDir, IEnumerable<IBuildTask> buildTasks)
      => ExecuteImpl(buildDir, buildTasks, new Dictionary<string, IBuildTask>());

    private static EntireBuildResult ExecuteImpl(string buildDir, IEnumerable<IBuildTask> buildTasks,
                                                 Dictionary<string, IBuildTask> outputFilesToTasks) {
      foreach (var buildTask in buildTasks) {
        ExecuteImpl(buildDir, buildTask.Dependencies, outputFilesToTasks);
        var taskSignature = buildTask.Signature();
        var taskOutputDir = Combine(buildDir, "output_cache", taskSignature);
        if (Exists(taskOutputDir)) { } else {
          ExecuteBuildTask(buildTask, taskSignature, buildDir, taskOutputDir);
        }
        AssertNoClashes(outputFilesToTasks, buildTask, taskOutputDir);
      }
      return new EntireBuildResult(EnumerateFiles(buildDir, "*", AllDirectories).ToImmutableArray());
    }

    private static void ExecuteBuildTask(IBuildTask buildTask, string taskSignature, string buildDir, string taskOutputDir) {
      var taskUnfinishedOutputDir = Combine(buildDir, "unfinished_output", taskSignature);
      CreateDirectory(taskUnfinishedOutputDir);
      buildTask.Execute(taskUnfinishedOutputDir);
      CreateDirectory(Combine(buildDir, "output_cache"));
      Move(taskUnfinishedOutputDir, taskOutputDir);
    }

    private static void AssertNoClashes(Dictionary<string, IBuildTask> outputFilesToTasks, IBuildTask buildTask, string taskOutputDir) {
      var outputFiles = GetTaskRelativeOutputFiles(taskOutputDir).ToList();
      foreach (var outputFile in outputFiles) {
        IBuildTask otherTask;
        if (outputFilesToTasks.TryGetValue(outputFile, out otherTask)) {
          throw new Exception($"Tasks '{otherTask.Name}' and '{buildTask.Name}' are clashing. " +
                              $"They produced the same file '{outputFile}'.");
        }
        outputFilesToTasks.Add(outputFile, buildTask);
      }
    }

    private static IEnumerable<string> GetTaskRelativeOutputFiles(string taskOutputDir) {
      var taskOutputDirUri = new Uri($"{taskOutputDir}/");
      return EnumerateFiles(taskOutputDir, "*", AllDirectories)
        .Select(path => taskOutputDirUri.MakeRelativeUri(new Uri(path)).ToString());
    }
  }

  public interface IBuildTask {
    void Execute(string buildDir);
    ImmutableArray<IBuildTask> Dependencies { get; }
    string Name { get; }
    string Signature();
  }

  public class TaskResult { }

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