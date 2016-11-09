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
    private const string BuildMetaDirName = ".bud";

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="task">the task that describes the build.</param>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="baseDir">the directory in which the build is to be executed. By default the current working
    /// directory is used.</param>
    /// <param name="metaDir">the directory where meta information about the build system is stored. By default
    /// the subdirectory `.bud` in the base directory is used.</param>
    public static void RunBuild(BuildTask task, TextWriter stdout = null, string baseDir = null, string metaDir = null)
      => RunBuild(new[] {task}, stdout, baseDir, metaDir);

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="tasks">the tasks that describe the build.</param>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="baseDir">the directory in which the build is to be executed. By default the current working
    /// directory is used.</param>
    /// <param name="metaDir">the directory where meta information about the build system is stored. By default
    /// the subdirectory `.bud` in the base directory is used.</param>
    public static void RunBuild(IEnumerable<BuildTask> tasks, TextWriter stdout = null, string baseDir = null,
                                string metaDir = null) {
      var buildTasks = tasks as IList<BuildTask> ?? tasks.ToList();
      var buildStopwatch = new Stopwatch();
      buildStopwatch.Start();
      var taskNumberAssigner = new TaskNumberAssigner(CountTasks(buildTasks));
      baseDir = baseDir ?? Directory.GetCurrentDirectory();
      metaDir = metaDir ?? Path.Combine(baseDir, BuildMetaDirName);
      var task2TaskGraphs = new Dictionary<BuildTask, TaskGraph>();
      var builtTaskGraphs = buildTasks.Select(task => ToTaskGraph(task2TaskGraphs, stdout, task, taskNumberAssigner,
                                                                  buildStopwatch, baseDir, metaDir));
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
    public static BuildActionTask Build(BuildAction action, string name = null, IEnumerable<BuildTask> dependsOn = null)
      => new BuildActionTask(action, name, dependsOn);

    /// <summary>
    ///   Creates a build task where multiple sources are built into multiple output files.
    /// </summary>
    /// <param name="command">this function performs the actual build.</param>
    /// <param name="sources">a glob pattern that will match files in the base directory (the directory in which
    /// the build was executed).</param>
    /// <param name="outputDir">the directory where output files will be placed.</param>
    /// <param name="outputExt">the extension of output files.</param>
    /// <param name="signature">see <see cref="BuildGlobToExtTask.Signature"/></param>
    /// <param name="dependsOn">other build tasks that this task depends on.</param>
    /// <returns>a build task that can be executed or can be used as a dependency of another task.</returns>
    public static BuildGlobToExtTask Build(BuildGlobToExtCommand command, string sources, string outputDir,
                                           string outputExt, string signature = null,
                                           IEnumerable<BuildTask> dependsOn = null)
      => new BuildGlobToExtTask(command, sources, outputDir, outputExt, signature, dependsOn);

    private static int CountTasks(ICollection<BuildTask> tasks)
      => tasks.Count + tasks.Select(task => CountTasks(task.Dependencies)).Sum();

    private static TaskGraph ToTaskGraph(Dictionary<BuildTask, TaskGraph> task2TaskGraphs, TextWriter stdout,
                                         BuildTask buildTask, TaskNumberAssigner taskNumberAssigner,
                                         Stopwatch buildStopwatch, string baseDir, string metaDir) {
      TaskGraph taskGraph;
      return task2TaskGraphs.TryGetValue(buildTask, out taskGraph) ?
               taskGraph :
               CreateTaskGraph(task2TaskGraphs, stdout, buildTask, taskNumberAssigner, buildStopwatch, baseDir,
                               metaDir);
    }

    private static TaskGraph CreateTaskGraph(Dictionary<BuildTask, TaskGraph> task2TaskGraphs, TextWriter stdout,
                                             BuildTask buildTask, TaskNumberAssigner taskNumberAssigner,
                                             Stopwatch buildStopwatch, string baseDir, string metaDir) {
      var taskGraphs = buildTask.Dependencies
                                .Select(dependencyBuildTask => ToTaskGraph(task2TaskGraphs, stdout, dependencyBuildTask,
                                                                           taskNumberAssigner, buildStopwatch, baseDir,
                                                                           metaDir))
                                .ToImmutableArray();
      var buildContext = new BuildContext(stdout, buildStopwatch, taskNumberAssigner.AssignNumber(),
                                          taskNumberAssigner.TotalTasks, baseDir);
      var taskGraph = new TaskGraph(() => buildTask.Execute(buildContext), taskGraphs);
      task2TaskGraphs.Add(buildTask, taskGraph);
      return taskGraph;
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