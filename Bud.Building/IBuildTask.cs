using System.Collections.Immutable;

namespace Bud {
  /// <summary>
  ///   The basic interface of all build tasks.
  /// </summary>
  public interface IBuildTask {
    /// <summary>
    /// Other build tasks that should be invoked before this one.
    /// </summary>
    ImmutableArray<IBuildTask> Dependencies { get; }

    /// <summary>
    ///   This method should perform the work of this build task.
    /// </summary>
    /// <param name="ctx">
    ///   This object contains a bunch of information for this task. Note: each task gets its own context.
    /// </param>
    /// <remarks>
    ///   This method is blocking.
    /// </remarks>
    void Execute(BuildContext ctx);
  }
}