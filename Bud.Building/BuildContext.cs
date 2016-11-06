using System.Diagnostics;
using System.IO;

namespace Bud {
  /// <summary>
  ///   Provides a bunch of information to each task.
  /// </summary>
  public class BuildContext {
    /// <param name="stdout"><see cref="Stdout"/></param>
    /// <param name="buildStopwatch"><see cref="BuildStopwatch"/></param>
    /// <param name="thisTaskNumber"><see cref="ThisTaskNumber"/></param>
    /// <param name="totalTasks"><see cref="TotalTasks"/></param>
    public BuildContext(TextWriter stdout, Stopwatch buildStopwatch, int thisTaskNumber, int totalTasks) {
      Stdout = stdout;
      BuildStopwatch = buildStopwatch;
      ThisTaskNumber = thisTaskNumber;
      TotalTasks = totalTasks;
    }

    /// <summary>
    ///   The output to which to write all output of this build task.
    /// </summary>
    public TextWriter Stdout {get;}

    /// <summary>
    ///   this stopwatch will be stopping time since the moment the user invoked
    ///   the <see cref="Building.RunBuild(System.IO.TextWriter,Bud.BuildActionTask[])"/> function.
    /// </summary>
    public Stopwatch BuildStopwatch {get;}

    /// <summary>
    ///   The number of the task to which this context belongs. Every build task in the task graph is
    ///   assigned a number with the following properties:
    ///
    ///   <para>
    ///     - every task number number is a positive integer between 1 and the total number of tasks,
    ///   </para>
    ///
    ///   <para>
    ///     - no two tasks share the same number, and
    ///   </para>
    ///
    ///   <para>
    ///     - the task number of a task is larger than any of the task numbers of its dependencies.
    ///   </para>
    /// </summary>
    public int ThisTaskNumber {get;}

    /// <summary>
    ///   the total number of tasks in a build graph that is currently being executed.
    /// </summary>
    public int TotalTasks {get;}
  }
}