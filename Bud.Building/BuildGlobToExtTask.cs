using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Bud.FileUtils;

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
    public string Signature { get; }

    /// <summary>
    ///    Creates a new build task.
    /// </summary>
    public BuildGlobToExtTask(BuildGlobToExtCommand command,
                              string sourceDir,
                              string sourceExt,
                              string outputDir,
                              string outputExt,
                              string signature = null,
                              IEnumerable<BuildTask> dependencies = null) : base(dependencies) {
      Command = command;
      SourceDir = sourceDir;
      SourceExt = sourceExt;
      OutputDir = outputDir;
      OutputExt = outputExt;
      Signature = signature;
    }

    /// <inheritdoc />
    public override void Execute(BuildContext ctx) {
      var sourceDir = ToAbsDir(SourceDir, ctx.BaseDir);
      var sources = FindFiles(sourceDir, SourceExt);
      var rootDirUri = new Uri($"{sourceDir}/");
      var outputDir = Path.Combine(ctx.BaseDir, OutputDir);

      var expectedOutputFiles = new HashSet<string>(
        sources.Select(src => rootDirUri.MakeRelativeUri(new Uri(src)).ToString())
               .Select(relativePath => ToOutputPath(outputDir, relativePath)));

      DeleteExtraneousFiles(outputDir, expectedOutputFiles, OutputExt);

      var hexSignature = HexUtils.ToHexStringFromBytes(CalculateTaskSignature(sources));

      Action command = () => {
        var buildGlobToExtContext = new BuildGlobToExtContext(ctx, sources, sourceDir, SourceExt, outputDir, OutputExt);
        Command(buildGlobToExtContext);
      };

      // NOTE: Maybe move this method into ctx.
      InvokeIfNeeded(command, expectedOutputFiles, ctx.TaskSignaturesDir,
                     hexSignature);

      ctx.MarkTaskFinished(this, hexSignature);
    }

    private string ToOutputPath(string outputDir, string relativePath)
      => Path.Combine(outputDir,
                      Path.GetDirectoryName(relativePath),
                      Path.GetFileNameWithoutExtension(relativePath) + OutputExt);

    private static void InvokeIfNeeded(Action command, IEnumerable<string> expectedOutputFiles, string signaturesDir,
                                       string hexDigest) {
      var taskSignatureFile = Path.Combine(signaturesDir, hexDigest);

      if (expectedOutputFiles.All(File.Exists) && File.Exists(taskSignatureFile)) {
        return;
      }

      command();

      Directory.CreateDirectory(signaturesDir);
      File.WriteAllBytes(taskSignatureFile, Array.Empty<byte>());
    }

    private byte[] CalculateTaskSignature(IEnumerable<string> sources)
      => new TaskSigner().DigestSources(sources).Digest(OutputDir).Finish().Signature;
  }
}