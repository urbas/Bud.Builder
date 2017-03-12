using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using static Bud.FileUtils;
using static Bud.HexUtils;

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
  ///     - the <see cref="Salt"/> of the task has changed since the last build.
  ///   </para>
  /// </remarks>
  public class GlobBuildTask : BuildTask {
    /// <summary>
    ///   This delegate performs the actual build.
    /// </summary>
    public GlobBuildCommand Command { get; }

    /// <summary>
    /// The directory in which to search for sources. If this directory is relative, then it will be resolved against
    /// the the base directory of the build (see <see cref="BuildContext.BaseDir"/>).
    /// </summary>
    public string SourceDir { get; }

    /// <summary>
    /// The extension of source files.
    /// </summary>
    public string SourceExt { get; }

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
    /// <remarks>
    ///   This parameter can be used to invalidate any old output. For example, say you invoked a build task that
    ///   runs the Foobarize compiler of version 1.0.0 with arguments <c>--optimize --minify</c>. In this case, you
    ///   could use a salt like this: <c>"Foobarize 1.0.0 opt+min"</c>. Now you upgrade the compiler to version 2.0.0
    ///   and run it with the argument <c>--debug</c>. In this case your salt parameter could look something like this:
    ///   <c>"Foobarize 2.0.0 debug"</c>. The build will notice that the salt has changed since the last run and will
    ///   re-invoke the compiler even if no source files have changed.
    /// </remarks>
    public string Salt { get; }

    /// <summary>
    ///    Creates a new build task.
    /// </summary>
    public GlobBuildTask(GlobBuildCommand command,
                         string sourceDir,
                         string sourceExt,
                         string outputDir,
                         string outputExt,
                         string salt = null,
                         IEnumerable<BuildTask> dependencies = null) : base(dependencies) {
      Command = command;
      SourceDir = sourceDir;
      SourceExt = sourceExt;
      OutputDir = outputDir;
      OutputExt = outputExt;
      Salt = salt;
    }

    /// <inheritdoc />
    public override BuildResult ExpectedResult(BuildContext ctx) {
      var absoluteSourceDir = AbsoluteSourceDir(ctx);
      var absoluteOutputDir = AbsoluteOutputDir(ctx);
      var rootDirUri = new Uri($"{absoluteSourceDir}/");

      var sources = FindFilesByExt(absoluteSourceDir, SourceExt).ToImmutableSortedSet();

      var expectedOutputFiles = sources.Select(src => rootDirUri.MakeRelativeUri(new Uri(src)).ToString())
                                       .Select(relativePath => ToOutputPath(absoluteOutputDir, relativePath))
                                       .ToImmutableHashSet();

      var hexSignature = ToHexStringFromBytes(CalculateTaskSignature(sources));

      return new BuildResult(inputFiles: sources, outputFiles: expectedOutputFiles, signature: hexSignature);
    }

    /// <inheritdoc />
    public override void Execute(BuildContext ctx, BuildResult expectedBuildResult) {
      var absoluteSourceDir = AbsoluteSourceDir(ctx);
      var absoluteOutputDir = AbsoluteOutputDir(ctx);

      DeleteExtraneousFiles(absoluteOutputDir, expectedBuildResult.OutputFiles, OutputExt);

      var globBuildContext = new GlobBuildContext(ctx, expectedBuildResult.InputFiles, absoluteSourceDir, SourceExt,
                                                  absoluteOutputDir, OutputExt);

      Command(globBuildContext);
    }

    private string AbsoluteOutputDir(IBuildContext ctx) => Path.Combine(ctx.BaseDir, OutputDir);

    private string AbsoluteSourceDir(IBuildContext ctx) => ToAbsDir(SourceDir, ctx.BaseDir);

    private string ToOutputPath(string outputDir, string relativePath)
      => Path.Combine(outputDir,
                      Path.GetDirectoryName(relativePath),
                      Path.GetFileNameWithoutExtension(relativePath) + OutputExt);

    /// <returns>a string of the form <c>"src/**/*.ts -> out/bin/**/*.js"</c></returns>
    public override string ToString() => $"{SourceDir}/**/*{SourceExt} -> {OutputDir}/**/*{OutputExt}";

    private ImmutableArray<byte> CalculateTaskSignature(IEnumerable<string> sources)
      => new Sha256Signer().Digest("Sources")
                           .DigestSources(sources)
                           .Digest("SourceDir")
                           .Digest(SourceDir)
                           .Digest("SourceExt")
                           .Digest(SourceExt)
                           .Digest("OutputDir")
                           .Digest(OutputDir)
                           .Digest("OutputExt")
                           .Digest(OutputExt)
                           .Finish()
                           .Signature;
  }
}