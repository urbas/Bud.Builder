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
    /// <param name="sourceDir">the directory from which the build task should draw its sources.</param>
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
    ///   A string that can be a filename and is also URL-safe (for example, it could be a hex string, or a Base64 URL
    ///   string).
    /// </returns>
    /// <remarks>
    /// This signature should be a cryptographically strong digest (e.g.: SHA256) of the task's inputs and other factors
    /// that affect the task's output. Some examples of potential digest inputs:
    /// 
    /// <ul>
    ///   <li>source files,</li>
    ///   <li>signatures of the task's dependncies,</li>
    ///   <li>environment variables,</li>
    ///   <li>version of the task's build algorithm,</li>
    /// </ul>
    /// 
    /// Bud.Builder assumes that the output of the task will be the same exactly when the signature of the task is the
    /// same.
    /// </remarks>
    string Signature(string sourceDir, ImmutableArray<BuildTaskResult> dependencyResults);
  }
}
