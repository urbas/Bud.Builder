using System.Collections.Immutable;

namespace Bud {
  public interface IBuildTask {
    void Execute(BuildTaskContext context, ImmutableArray<BuildTaskResult> dependencyResults);
    ImmutableArray<IBuildTask> Dependencies { get; }
    string Name { get; }

    /// <param name="ctx">this object contains the source directory of the current build, the output directory where
    ///   this task should place its files, and other information about the current build.</param>
    /// <param name="dependencyResults">the results of dependent build tasks.</param>
    /// <returns>
    ///   A hex string or a URL- and filename-safe Base64 string (i.e.: base64url). This signature should be a
    ///   cryptographically strong digest of the tasks inputs such as source files, signatures of dependncies,
    ///   environment variables, the task's algorithm, and other factors that affect the task's output.
    /// </returns>
    string GetSignature(string ctx, ImmutableArray<BuildTaskResult> dependencyResults);
  }
}