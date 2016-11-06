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
    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="buildTasks">the tasks that describe the build.</param>
    public static void RunBuild(TextWriter stdout, params BuildActionTask[] buildTasks) {
      var buildStopwatch = new Stopwatch();
      buildStopwatch.Start();
      var taskNumberAssigner = new TaskNumberAssigner(CountTasks(buildTasks));
      var builtTaskGraphs = buildTasks.Select(buildTask => ToTaskGraph(stdout,
                                                                       buildTask,
                                                                       taskNumberAssigner,
                                                                       buildStopwatch));
      new TaskGraph(builtTaskGraphs).Run();
    }

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="buildTasks">the tasks that describe the build.</param>
    public static void RunBuild(params BuildActionTask[] buildTasks) => RunBuild(Console.Out, buildTasks);

    /// <summary>
    ///   Creates a build task that will invoke the given action.
    /// </summary>
    /// <param name="action">the action to invoke in this build task.</param>
    /// <param name="name">
    ///   the name of this build task (this name will be used in the build output and build logs).
    /// </param>
    /// <param name="dependsOn">other build tasks that should be invoked before this build task.</param>
    /// <returns>the build task object.</returns>
    public static BuildActionTask Build(BuildAction action,
                                        string name = null,
                                        IEnumerable<BuildActionTask> dependsOn = null)
      => new BuildActionTask(action, name, dependsOn);

    private static int CountTasks(IReadOnlyCollection<BuildActionTask> buildTasks)
      => buildTasks.Count + buildTasks.Select(task => CountTasks(task.Dependencies)).Sum();

    private static TaskGraph ToTaskGraph(TextWriter stdout,
                                         BuildActionTask buildTask,
                                         TaskNumberAssigner taskNumberAssigner,
                                         Stopwatch buildStopwatch) {
      var taskGraphs = buildTask.Dependencies
                                .Select(dependencyBuildTask => ToTaskGraph(stdout,
                                                                           dependencyBuildTask,
                                                                           taskNumberAssigner,
                                                                           buildStopwatch))
                                .ToImmutableArray();
      var thisTaskNumber = taskNumberAssigner.AssignNumber();
      return new TaskGraph(
        () => buildTask.Invoke(stdout, buildStopwatch, thisTaskNumber, taskNumberAssigner.TotalTasks),
        taskGraphs);
    }
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