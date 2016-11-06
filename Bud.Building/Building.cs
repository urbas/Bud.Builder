using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Bud {
  /// <summary>
  ///   Provides static functions for defining and executing builds.
  /// </summary>
  public static class Building {
    private const int ReservedTimeStringLength = 7;

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="buildTasks">the tasks that describe the build.</param>
    public static void RunBuild(TextWriter stdout, params BuildTask[] buildTasks) {
      var buildStopwatch = new Stopwatch();
      buildStopwatch.Start();
      int totalTasks = CountTasks(buildTasks);
      var taskNumberAssigner = new TaskNumberAssigner(totalTasks);
      var builtTaskGraphs = buildTasks.Select(buildTask => ToTaskGraph(stdout, buildTask, taskNumberAssigner, buildStopwatch));
      var rootTaskGraph = new TaskGraph(builtTaskGraphs);
      rootTaskGraph.Run();
    }

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="buildTasks">the tasks that describe the build.</param>
    public static void RunBuild(params BuildTask[] buildTasks) => RunBuild(Console.Out, buildTasks);

    /// <summary>
    ///   Creates a build task that will invoke the given action.
    /// </summary>
    /// <param name="action">the action to invoke in this build task.</param>
    /// <param name="name">
    ///   the name of this build task (this name will be used in the build output and build logs).
    /// </param>
    /// <param name="dependsOn">other build tasks that should be invoked before this build task.</param>
    /// <returns>the build task object.</returns>
    public static BuildTask Build(BuildAction action, string name = null, IEnumerable<BuildTask> dependsOn = null)
      => new BuildTask(action, name, dependsOn);

    private static int CountTasks(IReadOnlyCollection<BuildTask> buildTasks)
      => buildTasks.Count + buildTasks.Select(task => CountTasks(task.Dependencies)).Sum();

    private static TaskGraph ToTaskGraph(TextWriter stdout, BuildTask buildTask, TaskNumberAssigner taskNumberAssigner, Stopwatch buildStopwatch) {
      var taskGraphs = buildTask.Dependencies
                                .Select(dependencyBuildTask => ToTaskGraph(stdout, dependencyBuildTask, taskNumberAssigner, buildStopwatch))
                                .ToImmutableArray();
      var thisTaskNumber = taskNumberAssigner.AssignNumber();
      return new TaskGraph(
        () => {
          LogBuildStart(stdout, buildTask, taskNumberAssigner, buildStopwatch, thisTaskNumber);
          buildTask.Action(new BuildActionContext());
          LogBuildEnd(stdout, buildTask, taskNumberAssigner, buildStopwatch, thisTaskNumber);
        },
        taskGraphs);
    }

    private static void LogBuildStart(TextWriter stdout, BuildTask buildTask, TaskNumberAssigner taskNumberAssigner, Stopwatch buildStopwatch, int thisTaskNumber) {
      var buildStartMessage = string.IsNullOrEmpty(buildTask.Name) ? "Started building." : $"Started building '{buildTask.Name}'.";
      WriteLogLine(stdout, thisTaskNumber, buildStopwatch, buildStartMessage, taskNumberAssigner.TotalTasks);
    }

    private static void LogBuildEnd(TextWriter stdout, BuildTask buildTask, TaskNumberAssigner taskNumberAssigner, Stopwatch buildStopwatch, int thisTaskNumber) {
      var buildDoneMessage = string.IsNullOrEmpty(buildTask.Name) ? "Done building." : $"Done building '{buildTask.Name}'.";
      WriteLogLine(stdout, thisTaskNumber, buildStopwatch, buildDoneMessage, taskNumberAssigner.TotalTasks);
    }

    private static void WriteLogLine(TextWriter logWriter, int taskNumber, Stopwatch stopwatch, string msg, int totalTasks)
      => logWriter.WriteLine("[{0}/{1} {2}s] {3}", taskNumber, totalTasks, GetTimestamp(stopwatch), msg);

    private static string GetTimestamp(Stopwatch buildStopwatch)
      => ((double) buildStopwatch.ElapsedMilliseconds/1000).ToString("F3").PadLeft(ReservedTimeStringLength);
  }

  internal class TaskNumberAssigner {
    private int lastAssignedNumber;

    public TaskNumberAssigner(int totalTasks) {
      TotalTasks = totalTasks;
    }

    public int TotalTasks { get; }

    public int AssignNumber() => ++lastAssignedNumber;
  }
}