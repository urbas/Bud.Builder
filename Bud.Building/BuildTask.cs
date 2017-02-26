using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bud {
  /// <summary>
  ///   The basic interface of all build tasks.
  /// </summary>
  public abstract class BuildTask {
    /// <summary>
    ///   Initializes a build task without dependencies.
    /// </summary>
    protected BuildTask() : this(null) { }

    /// <summary>
    ///   Initializes a build task with the given the dependencies.
    /// </summary>
    /// <param name="dependencies">these tasks will be executed before this task.</param>
    protected BuildTask(IEnumerable<BuildTask> dependencies)
      : this(dependencies == null ? ImmutableArray<BuildTask>.Empty : ImmutableArray.CreateRange(dependencies)) { }

    /// <summary>
    ///   Initializes a build task with the given the dependencies.
    /// </summary>
    /// <param name="dependencies">these tasks will be executed before this task.</param>
    protected BuildTask(ImmutableArray<BuildTask> dependencies) {
      Dependencies = dependencies;
    }

    /// <summary>
    /// Other build tasks that should be invoked before this one.
    /// </summary>
    public ImmutableArray<BuildTask> Dependencies { get; }

    /// <summary>
    ///   This method should perform the work of this build task.
    /// </summary>
    /// <param name="ctx">
    ///   This object contains a bunch of information for this task. Note: each task gets its own context.
    /// </param>
    /// <remarks>
    ///   This method is blocking.
    /// </remarks>
    public abstract BuildResult Execute(BuildContext ctx);
  }
}