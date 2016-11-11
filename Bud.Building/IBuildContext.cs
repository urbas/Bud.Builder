using System.Diagnostics;
using System.IO;

namespace Bud {
  /// <summary>
  ///   Each task will get an instance of this type as a parameter when it is executed.
  /// </summary>
  public interface IBuildContext {
    /// <summary>
    ///   The output to which to write all output of this build task.
    /// </summary>
    TextWriter Stdout { get; }

    /// <summary>
    ///   this stopwatch will be stopping time since the moment the user invoked
    ///   the <see cref="Building.RunBuild(Bud.BuildTask,System.IO.TextWriter,string,string)"/> function.
    /// </summary>
    Stopwatch BuildStopwatch { get; }

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
    int ThisTaskNumber { get; }

    /// <summary>
    ///   the total number of tasks in a build graph that is currently being executed.
    /// </summary>
    int TotalTasks { get; }

    /// <summary>
    ///   This is the directory where the build was executed.
    /// </summary>
    string BaseDir { get; }
  }
}