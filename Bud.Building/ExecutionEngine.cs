using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Bud {
  internal class ExecutionEngine {
    private readonly TextWriter stdout;
    private readonly IList<BuildTask> buildTasks;
    private readonly string baseDir;
    private readonly string metaDir;
    private readonly IDictionary<BuildTask, TaskGraph> task2TaskGraphs = new Dictionary<BuildTask, TaskGraph>();

    private readonly ConcurrentDictionary<string, BuildTask> signatures2Tasks =
      new ConcurrentDictionary<string, BuildTask>();

    private readonly ConcurrentDictionary<string, BuildTask> outputFiles2Tasks =
      new ConcurrentDictionary<string, BuildTask>();

    private readonly Stopwatch buildStopwatch = new Stopwatch();
    private readonly int totalTasks;
    private int lastAssignedNumber;

    public ExecutionEngine(TextWriter stdout, IList<BuildTask> buildTasks, string baseDir, string metaDir) {
      this.stdout = stdout;
      this.buildTasks = buildTasks;
      this.baseDir = baseDir;
      this.metaDir = metaDir;
      totalTasks = CountTasks(buildTasks);
    }

    public void ExecuteBuild() {
      buildStopwatch.Start();

      try {
        new TaskGraph(buildTasks.Select(GetOrCreateTaskGraph)).Run();
      } finally {
        var taskSignaturesDir = Path.Combine(baseDir, BuildExecution.TaskSignaturesDirName);
        FileUtils.DeleteExtraneousFiles(taskSignaturesDir, ToSignatureFiles(signatures2Tasks.Keys, taskSignaturesDir));
      }
    }

    private TaskGraph GetOrCreateTaskGraph(BuildTask task) {
      TaskGraph taskGraph;
      return task2TaskGraphs.TryGetValue(task, out taskGraph) ? taskGraph : CreateTaskGraph(task);
    }

    private TaskGraph CreateTaskGraph(BuildTask buildTask) {
      var dependenciesTaskGraphs = buildTask.Dependencies.Select(GetOrCreateTaskGraph).ToImmutableArray();
      var buildContext = new BuildContext(stdout, buildStopwatch, NextTaskNumber(), totalTasks, baseDir);

      var taskGraph = new TaskGraph(() => {
        var expectedBuildResult = buildTask.ExpectedResult(buildContext);
        RegisterOutputFiles(outputFiles2Tasks, buildTask, expectedBuildResult.OutputFiles);

        var taskSignatureFile = Path.Combine(buildContext.TaskSignaturesDir, expectedBuildResult.Signature);
        if (!expectedBuildResult.OutputFiles.All(File.Exists) || !File.Exists(taskSignatureFile)) {
          buildTask.Execute(buildContext, expectedBuildResult);
        }

        MarkTaskFinished(signatures2Tasks, buildTask, expectedBuildResult.Signature);
        Directory.CreateDirectory(buildContext.TaskSignaturesDir);
        File.WriteAllBytes(taskSignatureFile, Array.Empty<byte>());
      }, dependenciesTaskGraphs);
      task2TaskGraphs.Add(buildTask, taskGraph);
      return taskGraph;
    }

    private static IImmutableSet<string> ToSignatureFiles(IEnumerable<string> signatures, string taskSignaturesDir)
      => signatures.Select(signature => Path.Combine(taskSignaturesDir, signature))
                   .ToImmutableHashSet();

    private static int CountTasks(ICollection<BuildTask> tasks)
      => tasks.Count + tasks.Select(task => CountTasks(task.Dependencies)).Sum();

    private int NextTaskNumber() => ++lastAssignedNumber;

    /// <summary>
    ///   This method will check that the output files of one build task don't clash with output files of any other
    ///   build task.
    /// </summary>
    /// <param name="outputFiles2Tasks">all output files produced so far.</param>
    /// <param name="buildTask">the task that produces the given output files belong.</param>
    /// <param name="outputFiles">the output files that the given build task produces.</param>
    /// <exception cref="Exception">this exception is thrown if any of the output files clashes with the output files
    /// of another build task.</exception>
    private static void RegisterOutputFiles(ConcurrentDictionary<string, BuildTask> outputFiles2Tasks,
                                            BuildTask buildTask, IEnumerable<string> outputFiles) {
      var fullOutputPaths = outputFiles.Select(Path.GetFullPath);
      foreach (var fullOutputPath in fullOutputPaths) {
        var existingTask = outputFiles2Tasks.GetOrAdd(fullOutputPath, buildTask);
        if (existingTask != buildTask) {
          throw new Exception($"Two builds are trying to produce the file '{fullOutputPath}'. " +
                              $"Build '{existingTask}' and '{buildTask}'.");
        }
      }
    }

    /// <summary>
    /// Adds the signature and the task into a build-wide dictionary of signatures and corresponding tasks. If the
    /// signature already exists for a different task, then this method throws an exception.
    /// </summary>
    /// <param name="signatures2Tasks">all task signatures collected so far</param>
    /// <param name="buildTask">the task that successfully finished.</param>
    /// <param name="taskSignature">the signature of the given <paramref name="buildTask"/>.</param>
    /// <exception cref="Exception">thrown if another build task with the same signature already finished
    /// before.</exception>
    private static void MarkTaskFinished(ConcurrentDictionary<string, BuildTask> signatures2Tasks,
                                         BuildTask buildTask,
                                         string taskSignature) {
      var storedTask = signatures2Tasks.GetOrAdd(taskSignature, buildTask);
      if (storedTask != buildTask) {
        throw new Exception($"Clashing build specification. Found duplicate tasks: '{storedTask}' and '{buildTask}'.");
      }
    }
  }
}