using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bud {
  /// <summary>
  ///   This class represents an atomic unit of work in a build.
  /// </summary>
  public class BuildActionTask : BuildTask {
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
    ///   Creates a new build task.
    /// </summary>
    /// <param name="action"><see cref="Action"/></param>
    /// <param name="name"><see cref="Name"/></param>
    /// <param name="dependencies"><see cref="BuildTask.Dependencies"/></param>
    public BuildActionTask(BuildAction action, string name = null, IEnumerable<BuildTask> dependencies = null)
      : base(dependencies) {
      Action = action;
      Name = name;
    }

    public override ImmutableArray<string> OutputFiles => ImmutableArray<string>.Empty;

    /// <inheritdoc />
    public override void Execute(BuildContext ctx) {
      LogMessages.LogBuildStart(ctx, Name);
      Action(ctx);
      LogMessages.LogBuildEnd(ctx, Name);
    }
  }
}