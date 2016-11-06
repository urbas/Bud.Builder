using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bud {
  /// <summary>
  ///   This class represents an atomic unit of work in a build.
  /// </summary>
  public class BuildActionTask : IBuildTask {
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

    /// <inheritdoc />
    public ImmutableArray<IBuildTask> Dependencies { get; }

    /// <summary>
    ///   Creates a new build task.
    /// </summary>
    /// <param name="action"><see cref="Action"/></param>
    /// <param name="name"><see cref="Name"/></param>
    /// <param name="dependencies"><see cref="Dependencies"/></param>
    public BuildActionTask(BuildAction action, string name = null, IEnumerable<IBuildTask> dependencies = null) {
      Action = action;
      Name = name;
      Dependencies = dependencies == null ?
                       ImmutableArray<IBuildTask>.Empty :
                       ImmutableArray.CreateRange(dependencies);
    }

    /// <inheritdoc />
    public void Execute(BuildContext ctx) {
      LogMessages.LogBuildStart(ctx, Name);
      Action(ctx);
      LogMessages.LogBuildEnd(ctx, Name);
    }
  }
}