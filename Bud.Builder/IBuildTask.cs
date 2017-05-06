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
  ///   The builder decides based on a task's signature whether to invoke the task or reuse a previous
  ///   output.
  /// </para>
  /// </remarks>
  public interface IBuildTask {
    /// <summary>
    ///   This method does the bulk of the task's work. It produces the task's output.
    /// </summary>
    /// <param name="sourceDir">the source directory.</param>
    /// <param name="outputDir">the directory where this task should place all its output files.</param>
    /// <param name="dependencyResults">
    ///   this array contains the outcomes of tasks on which this task depends.
    /// </param>
    void Execute(string sourceDir, string outputDir, ImmutableArray<BuildTaskResult> dependencyResults);

    /// <summary>
    ///   Other tasks on which this task directly depends.
    /// </summary>
    ImmutableArray<IBuildTask> Dependencies { get; }

    /// <summary>
    ///   A human-readable name of the task. It must be unique in the build graph.
    /// </summary>
    string Name { get; }

    /// <param name="sourceDir">the source directory.</param>
    /// <param name="dependencyResults">this array contains the outcomes of tasks on which this task depends.</param>
    /// <returns>
    ///   A hex string or a URL- and filename-safe Base64 string (i.e.: base64url). This signature should be a
    ///   cryptographically strong digest of the tasks inputs such as source files, signatures of dependncies,
    ///   environment variables, the task's algorithm, and other factors that affect the task's output.
    /// </returns>
    string Signature(string sourceDir, ImmutableArray<BuildTaskResult> dependencyResults);
  }
}