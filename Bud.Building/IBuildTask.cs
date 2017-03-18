using System.Collections.Immutable;

namespace Bud {
  /// <summary>
  ///   The basic building block of a build.
  /// </summary>
  /// <remarks>
  /// <para>Build tasks take some input files and produce some output files.</para>
  ///
  /// <para>Each build task can depend on other build tasks.</para>
  ///
  /// <para>
  ///   Every build task must also follow some basic rules. Firstly,
  ///   every build task must be deterministic. This means that given the same input, it must produce the same output.
  ///   Secondly, for the same input, a build task must report the same signature. Finally, every build task must report
  ///   a different signature for different inputs.
  /// </para>
  ///
  /// <para>
  ///   The build execution engine decides based on a task's signature whether to invoke the task or reuse a previous
  ///   output.
  /// </para>
  /// </remarks>
  public interface IBuildTask {
    /// <summary>
    ///   This method does the bulk of the task's work. It produces the task's output.
    /// </summary>
    /// <param name="context">
    ///   this object provides the source directory (where this task can find its source files) and the output directory
    ///   (where this task should place its output files).
    /// </param>
    /// <param name="dependencyResults">
    ///   this array contains the outcomes of tasks on which this task depends.
    /// </param>
    void Execute(BuildTaskContext context, ImmutableArray<BuildTaskResult> dependencyResults);

    /// <summary>
    ///   Other tasks on which this task directly depends.
    /// </summary>
    ImmutableArray<IBuildTask> Dependencies { get; }

    /// <summary>
    ///   A human-readable designation of the task.
    /// </summary>
    string Name { get; }

    /// <param name="ctx">this object contains the source directory of the current build, the output directory where
    ///   this task should place its files, and other information about the current build.</param>
    /// <param name="dependencyResults">the results of dependent build tasks.</param>
    /// <returns>
    ///   A hex string or a URL- and filename-safe Base64 string (i.e.: base64url). This signature should be a
    ///   cryptographically strong digest of the tasks inputs such as source files, signatures of dependncies,
    ///   environment variables, the task's algorithm, and other factors that affect the task's output.
    /// </returns>
    string Signature(string ctx, ImmutableArray<BuildTaskResult> dependencyResults);
  }
}