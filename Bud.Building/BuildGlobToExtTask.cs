using System.Collections.Generic;

namespace Bud {
  /// <summary>This build task is suitable for builders/compilers that take multiple source files
  /// and produce multiple target files.</summary>
  /// <remarks>
  ///   Use this build task type if all of these conditions are satisfied:
  ///
  ///   <para>
  ///     - your builder (i.e.: compiler) takes a list of many source files or your tool unrestands Ant-style globs
  ///       (e.g.: <c>src/ts/**/*.ts</c>),
  ///   </para>
  ///
  ///   <para>
  ///     - your builder produces exactly one output file for each source file where each output file has the same
  ///       extension,
  ///   </para>
  ///
  ///   <para>
  ///     - all output files are placed in a chosen directory and the folder structure is preserved. For example,
  ///       say you specified sources with <c>src/ts/**/*.ts</c> and you chose output directory <c>build</c>, then
  ///       the file <c>src/ts/foo/bar/moo.ts</c> should end up in <c>build/foo/bar/moo.js</c>.
  ///   </para>
  ///
  ///   <para>
  ///     Note that the build command will not be called if a previous output is up to date. The output is
  ///     out-of-date, if any of the following is true:
  ///   </para>
  ///
  ///   <para>
  ///     - any of the output files are missing,
  ///   </para>
  ///
  ///   <para>
  ///     - the set of input source files has changed since the last build (order does not matter),
  ///   </para>
  ///
  ///   <para>
  ///     - the SHA256 hash of any of the input source files has changes since the last build (timestamps are
  ///       ignored), or
  ///   </para>
  ///
  ///   <para>
  ///     - the <see cref="Signature"/> of the task has changed since the last build.
  ///   </para>
  /// </remarks>
  public class BuildGlobToExtTask : BuildTask {
    /// <summary>
    ///   This delegate performs the actual build.
    /// </summary>
    public BuildGlobToExtCommand Command { get; }

    /// <summary>
    ///  This is a glob pattern through which to find files in the base directory. The found files will be the input
    ///  source files to the build command.
    /// </summary>
    public string Sources { get; }

    /// <summary>
    ///   The directory where all output files will be placed.
    /// </summary>
    public string OutputDir { get; }

    /// <summary>
    ///   The extension of output files.
    /// </summary>
    public string OutputExt { get; }

    /// <summary>
    ///   This string is used to determine whether the task has changed due to factors other than input and output
    ///   files.
    /// </summary>
    public string Signature { get; }

    /// <summary>
    ///    Creates a new build task.
    /// </summary>
    public BuildGlobToExtTask(BuildGlobToExtCommand command, string sources, string outputDir, string outputExt,
                              string signature = null, IEnumerable<BuildTask> dependencies = null)
      : base(dependencies) {
      Command = command;
      Sources = sources;
      OutputDir = outputDir;
      OutputExt = outputExt;
      Signature = signature;
    }

    /// <inheritdoc />
    public override void Execute(BuildContext ctx) {}
  }
}