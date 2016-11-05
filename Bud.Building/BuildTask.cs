using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Bud {
  /// <summary>
  ///   This class represents an atomic unit of work in a build.
  /// </summary>
  public class BuildTask {
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
    public ImmutableArray<BuildTask> Dependencies { get; }

    /// <summary>
    ///   Creates a new build task.
    /// </summary>
    /// <param name="action"><see cref="Action"/></param>
    /// <param name="name"><see cref="Name"/></param>
    /// <param name="dependencies"><see cref="Dependencies"/></param>
    public BuildTask(BuildAction action, string name, IEnumerable<BuildTask> dependencies) {
      Action = action;
      Name = name;
      Dependencies = ImmutableArray.CreateRange(dependencies ?? Enumerable.Empty<BuildTask>());
    }
  }
}