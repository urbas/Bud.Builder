using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Bud {
  /// <summary>
  ///   This class represents an atomic unit of work in a build.
  /// </summary>
  public class BuildActionTask {
    /// <summary>
    ///   This action does the actual work of this build task.
    /// </summary>
    public BuildAction Action { get; }

    /// <summary>
    ///   The name of this build task.
    /// </summary>
    /// <remarks>
    ///   This name will be used in the build output.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Other build tasks that should be invoked before this one.
    /// </summary>
    public ImmutableArray<BuildActionTask> Dependencies { get; }

    /// <summary>
    ///   Creates a new build task.
    /// </summary>
    /// <param name="action"><see cref="Action"/></param>
    /// <param name="name"><see cref="Name"/></param>
    /// <param name="dependencies"><see cref="Dependencies"/></param>
    public BuildActionTask(BuildAction action, string name, IEnumerable<BuildActionTask> dependencies) {
      Action = action;
      Name = name;
      Dependencies = ImmutableArray.CreateRange(dependencies ?? Enumerable.Empty<BuildActionTask>());
    }

    /// <summary>
    ///   This method should perform the work of this build task.
    /// </summary>
    /// <param name="stdout">the output to which to write all output of this build task.</param>
    /// <param name="buildStopwatch">
    ///   this stopwatch will be stopping time since the moment the user invoked
    ///   the <see cref="Building.RunBuild(System.IO.TextWriter,Bud.BuildActionTask[])"/> function.
    /// </param>
    /// <param name="thisTaskNumber">
    ///   the number of this task. Every build task in the task graph is assigned a number with the following
    ///   properties:
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
    /// </param>
    /// <param name="totalTasks">
    ///   the total number of tasks in a build graph that is currently being executed.
    /// </param>
    /// <remarks>
    ///   This method is blocking.
    /// </remarks>
    public void Invoke(TextWriter stdout, Stopwatch buildStopwatch, int thisTaskNumber, int totalTasks) {
      LogMessages.LogBuildStart(stdout, buildStopwatch, thisTaskNumber, totalTasks, this.Name);
      Action(new BuildActionContext());
      LogMessages.LogBuildEnd(stdout, buildStopwatch, thisTaskNumber, totalTasks, this.Name);
    }
  }
}