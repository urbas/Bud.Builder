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
  /// </remarks>
  public class BuildGlobToExtTask : BuildTask {
    /// <summary>
    ///    Creates a new build task.
    /// </summary>
    public BuildGlobToExtTask(BuildGlobToExtCommand command, string sources, string outputDir, string outputExt,
                              IEnumerable<BuildTask> dependencies = null)
      : base(dependencies) {}

    /// <inheritdoc />
    public override void Execute(BuildContext ctx) {
      throw new System.NotImplementedException();
    }
  }
}