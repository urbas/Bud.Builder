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
    /// <param name="task">the task that describes the build.</param>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="baseDir">the directory in which the build is to be executed.</param>
    public static void RunBuild(BuildTask task, TextWriter stdout = null, string baseDir = null)
      => RunBuild(new []{task}, stdout, baseDir);

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="tasks">the tasks that describe the build.</param>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="baseDir">the directory in which the build is to be executed.</param>
    public static void RunBuild(IEnumerable<BuildTask> tasks, TextWriter stdout = null, string baseDir = null) {
      var buildTasks = tasks as IList<BuildTask> ?? tasks.ToList();
      var buildStopwatch = new Stopwatch();
      buildStopwatch.Start();
      var taskNumberAssigner = new TaskNumberAssigner(CountTasks(buildTasks));
      var builtTaskGraphs = buildTasks.Select(task => ToTaskGraph(stdout, task, taskNumberAssigner, buildStopwatch));
      new TaskGraph(builtTaskGraphs).Run();
    }

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
                                        IEnumerable<BuildTask> dependsOn = null)
      => new BuildActionTask(action, name, dependsOn);

    public static BuildGlobToExtTask Build(BuildGlobToExtCommand command, string sources, string outputDir,
                                           string outputExt) {
      return null;
    }

    private static int CountTasks(ICollection<BuildTask> tasks)
      => tasks.Count + tasks.Select(task => CountTasks(task.Dependencies)).Sum();

    private static TaskGraph ToTaskGraph(TextWriter stdout,
                                         BuildTask buildTask,
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
        () => buildTask.Execute(new BuildContext(stdout, buildStopwatch, thisTaskNumber, taskNumberAssigner.TotalTasks)),
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