using System.Collections.Generic;
using System.Collections.Immutable;
using static Bud.Exec;

namespace Bud {
  /// <summary>
  /// This class provides build functions with information such as the list of sources that
  /// are supposed to be built, the output directory into which to place output files, the
  /// extension of files produced by the build, the logger, and some helper functions through which
  /// to invoke external compilers.
  /// </summary>
  public class GlobBuildContext {
    /// <summary>
    ///   Creates a new context with the given information.
    /// </summary>
    public GlobBuildContext(IImmutableSet<string> sources, string sourceDir, string sourcesExt, string outputDir,
                            string outputExt) {
      Sources = sources;
      SourceDir = sourceDir;
      SourcesExt = sourcesExt;
      OutputDir = outputDir;
      OutputExt = outputExt;
    }

    /// <summary>
    /// A helper function for executing external processes. The main purpose of this function is to log the output of
    /// the invoked process in the common format.
    /// </summary>
    /// <param name="executablePath">The executable to invoke.</param>
    /// <param name="args">The arguments to pass to the executable. You can use functions
    /// <see cref="Exec.Args(string[])"/> and <see cref="Exec.Arg"/> in this parameter.</param>
    /// <param name="cwd">the working directory where the process should run. If not given, <see cref="OutputDir"/> will
    /// be used.</param>
    /// <param name="env">the environment to pass to the process. If none given, the process will inherit the
    /// environment from this process.</param>
    /// <remarks>
    ///   This method throws if the process fails.
    /// </remarks>
    public void Command(string executablePath, string args = null, string cwd = null,
                        IDictionary<string, string> env = null) {
      Run(executablePath, args, cwd: cwd ?? OutputDir, env: env);
    }

    /// <summary>
    /// The directory into which the task will place output files.
    /// </summary>
    public string OutputDir { get; }

    /// <summary>
    /// The extension of output files.
    /// </summary>
    public string OutputExt { get; }

    /// <summary>
    /// The sources this task should build.
    /// </summary>
    public IImmutableSet<string> Sources { get; }

    /// <summary>
    ///   The directory where source files will be searched for.
    /// </summary>
    /// <remarks>
    ///   The directory relative to which paths of source files will be computed. This relative path will be equal to
    ///   the relative path of corresponding output files.
    /// </remarks>
    public string SourceDir { get; }

    /// <summary>
    ///   The extension of source files.
    /// </summary>
    public string SourcesExt { get; }
  }
}