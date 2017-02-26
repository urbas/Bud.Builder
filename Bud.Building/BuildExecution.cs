using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Bud.FileUtils;

namespace Bud {
  internal static class BuildExecution {
    public const string BuildMetaDirName = ".bud";
    public const string TaskSignaturesDirName = "task_signatures";

    internal static void RunBuild(IEnumerable<BuildTask> tasks,
                                  TextWriter stdout,
                                  string baseDir,
                                  string metaDir) {
      var buildTasks = tasks as IList<BuildTask> ?? tasks.ToList();
      var buildStopwatch = new Stopwatch();
      buildStopwatch.Start();
      var taskNumberAssigner = new BuildTaskNumberAssigner(CountTasks(buildTasks));
      baseDir = baseDir ?? Directory.GetCurrentDirectory();
      metaDir = metaDir ?? Path.Combine(baseDir, BuildMetaDirName);
      var task2TaskGraphs = new Dictionary<BuildTask, TaskGraph>();
      var signatures2Tasks = new ConcurrentDictionary<string, BuildTask>();
      var outputFiles2Tasks = new ConcurrentDictionary<string, BuildTask>();
      var builtTaskGraphs = buildTasks.Select(task => ToTaskGraph(task2TaskGraphs, stdout, task, taskNumberAssigner,
                                                                  buildStopwatch, baseDir, metaDir, signatures2Tasks,
                                                                  outputFiles2Tasks));

      try {
        new TaskGraph(builtTaskGraphs).Run();
      } finally {
        var taskSignaturesDir = Path.Combine(baseDir, TaskSignaturesDirName);
        DeleteExtraneousFiles(taskSignaturesDir,
                              new HashSet<string>(ToSignatureFiles(signatures2Tasks.Keys, taskSignaturesDir)));
      }
    }

    private static IEnumerable<string> ToSignatureFiles(IEnumerable<string> signatures, string taskSignaturesDir)
      => signatures.Select(signature => Path.Combine(taskSignaturesDir, signature));

    private static int CountTasks(ICollection<BuildTask> tasks)
      => tasks.Count + tasks.Select(task => CountTasks(task.Dependencies)).Sum();

    private static TaskGraph ToTaskGraph(IDictionary<BuildTask, TaskGraph> task2TaskGraphs, TextWriter stdout,
                                         BuildTask buildTask, BuildTaskNumberAssigner buildTaskNumberAssigner,
                                         Stopwatch buildStopwatch, string baseDir, string metaDir,
                                         ConcurrentDictionary<string, BuildTask> signatures2Tasks,
                                         ConcurrentDictionary<string, BuildTask> outputFiles2Tasks) {
      TaskGraph taskGraph;
      return task2TaskGraphs.TryGetValue(buildTask, out taskGraph) ?
               taskGraph :
               CreateTaskGraph(task2TaskGraphs, stdout, buildTask, buildTaskNumberAssigner, buildStopwatch, baseDir,
                               metaDir, signatures2Tasks, outputFiles2Tasks);
    }

    private static TaskGraph CreateTaskGraph(IDictionary<BuildTask, TaskGraph> task2TaskGraphs,
                                             TextWriter stdout,
                                             BuildTask buildTask,
                                             BuildTaskNumberAssigner buildTaskNumberAssigner,
                                             Stopwatch buildStopwatch,
                                             string baseDir,
                                             string metaDir,
                                             ConcurrentDictionary<string, BuildTask> signatures2Tasks,
                                             ConcurrentDictionary<string, BuildTask> outputFiles2Tasks) {
      var taskGraphs = buildTask.Dependencies
                                .Select(dependencyBuildTask => ToTaskGraph(task2TaskGraphs, stdout, dependencyBuildTask,
                                                                           buildTaskNumberAssigner, buildStopwatch,
                                                                           baseDir, metaDir, signatures2Tasks,
                                                                           outputFiles2Tasks))
                                .ToImmutableArray();
      var buildContext = new BuildContext(stdout, buildStopwatch, buildTaskNumberAssigner.AssignNumber(),
                                          buildTaskNumberAssigner.TotalTasks, baseDir, signatures2Tasks,
                                          outputFiles2Tasks);
      var taskGraph = new TaskGraph(() => {
        var buildResult = buildTask.Execute(buildContext);
        RegisterOutputFiles(outputFiles2Tasks, buildTask, buildResult.OutputFiles);
      }, taskGraphs);
      task2TaskGraphs.Add(buildTask, taskGraph);
      return taskGraph;
    }

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


    private class BuildTaskNumberAssigner {
      private int lastAssignedNumber;

      public BuildTaskNumberAssigner(int totalTasks) {
        TotalTasks = totalTasks;
      }

      public int TotalTasks { get; }

      public int AssignNumber() => ++lastAssignedNumber;
    }
  }
}