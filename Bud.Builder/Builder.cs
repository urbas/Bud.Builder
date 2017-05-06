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
  ///   This builder tries to isolate tasks by creating temporary output directories for each task.
  /// </summary>
  /// <remarks>
  ///   Here's how the builder works in more detail:
  ///
  ///   <ul>
  ///     <li>First execute all direct dependencies of a task.</li>
  ///
  ///     <li>Afterwards calculate the signature of the task.</li>
  ///
  ///     <li>Check that no other task has the same signature.</li>
  ///
  ///     <li>Check if an output directory with same signature already exists.</li>
  ///
  ///     <li>If the directory exists, go to the last step, if not, continue with the next step.</li>
  ///
  ///     <li>Create a temporary directory and tell the task to put its output into this directory.</li>
  ///
  ///     <li>If the task finishes successfully, move the temporary output directory to the cache of finished
  ///         outputs.</li>
  ///
  ///     <li>Abort the build if the task threw an exception.</li>
  ///
  ///     <li>Check that no two tasks produced the same output files.</li>
  ///
  ///     <li>Copy the contents of all output directories into the final output directory.</li>
  ///   </ul>
  ///
  ///   All output files will end up in <see cref="Builder.OutputDir"/>.
  /// </remarks>
  public class Builder {
    /// <summary>
    ///   The directory where all sources of the build are located.
    /// </summary>
    public string SourceDir { get; }

    /// <summary>
    ///   The directory where all output files should end up.
    /// </summary>
    public string OutputDir { get; }

    /// <summary>
    ///   The directory where this builder will place its build meta files, caches, and other internal files.
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

    private readonly ConcurrentDictionary<IBuildTask, BuildTaskResult> buildTasksToResults
      = new ConcurrentDictionary<IBuildTask, BuildTaskResult>(new Dictionary<IBuildTask, BuildTaskResult>());

    private readonly ConcurrentDictionary<string, IBuildTask> signatureToBuildTask
      = new ConcurrentDictionary<string, IBuildTask>();

    private Builder(string sourceDir, string outputDir, string metaDir) {
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
    /// <param name="metaDir">the directory in which the builder will store temporary build artifacts and
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
    /// <param name="metaDir">the directory in which the builder will store temporary build artifacts and
    /// build metadata.</param>
    /// <param name="buildTasks">the build tasks to execute.</param>
    ///  <returns>an object containing information about the resulting build.</returns>
    ///  <exception cref="Exception">this exception is thrown if the build fails for any reason.</exception>
    public static void Execute(string sourceDir, string outputDir, string metaDir, IEnumerable<IBuildTask> buildTasks)
      => new Builder(sourceDir, outputDir, metaDir).Execute(buildTasks);

    private void Execute(IEnumerable<IBuildTask> buildTasks) {
      CreateMetaOutputDirs();
      ExecuteBuildTasks(buildTasks);
      AssertNoClashingFiles();
      CopyToOutputDir();
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
        TaskGraph.ToTaskGraph(buildTasks,
                              task => task.Name,
                              task => task.Dependencies,
                              task => () => GraphNodeAction(task))
                 .Run();
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

    private void CopyToOutputDir() {
      if (Exists(OutputDir)) {
        Delete(OutputDir, true);
      }
      foreach (var taskOutputDir in TaskOutputDirs) {
        CopyTree(taskOutputDir, OutputDir);
      }
    }

    private IEnumerable<string> TaskOutputDirs
      => signatureToBuildTask.Keys.Select(sig => Combine(DoneOutputsDir, sig));

    private void GraphNodeAction(IBuildTask buildTask) {
      // At this point all dependencies will have been evaluated.
      var dependenciesResults = GetDependenciesResults(buildTask);
      var taskSignature = buildTask.Signature(SourceDir, dependenciesResults);
      AssertUniqueSignature(buildTask, taskSignature);
      var buildTaskResult = ExecuteBuildTask(buildTask, taskSignature, dependenciesResults);
      buildTasksToResults.TryAdd(buildTask, buildTaskResult);
    }

    private BuildTaskResult ExecuteBuildTask(IBuildTask buildTask,
                                             string taskSignature,
                                             ImmutableArray<BuildTaskResult> dependenciesResults) {
      var taskOutputDir = Combine(DoneOutputsDir, taskSignature);
      if (!Exists(taskOutputDir)) {
        var partialTaskOutputDir = Combine(PartialOutputsDir, taskSignature);
        CreateDirectory(partialTaskOutputDir);
        buildTask.Execute(SourceDir, partialTaskOutputDir, dependenciesResults);
        Move(partialTaskOutputDir, taskOutputDir);
      }
      return new BuildTaskResult(taskSignature, taskOutputDir, dependenciesResults);
    }

    private ImmutableArray<BuildTaskResult> GetDependenciesResults(IBuildTask buildTask)
      => buildTask.Dependencies.Select(task => buildTasksToResults[task]).ToImmutableArray();

    private void AssertUniqueSignature(IBuildTask buildTask, string taskSignature) {
      var storedTask = signatureToBuildTask.GetOrAdd(taskSignature, buildTask);
      if (storedTask != buildTask) {
        throw new Exception($"Tasks '{storedTask.Name}' and '{buildTask.Name}' are clashing. " +
                            $"They have the same signature '{taskSignature}'.");
      }
    }
  }
}