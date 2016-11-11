using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using static Bud.Exec;

namespace Bud {
  /// <summary>
  /// This class provides build functions with information such as the list of sources that
  /// are supposed to be built, the output directory into which to place output files, the
  /// extension of files produced by the build, the logger, and some helper functions through which
  /// to invoke external compilers.
  /// </summary>
  public class BuildGlobToExtContext : IBuildContext {
    public BuildGlobToExtContext(BuildContext ctx, ImmutableArray<string> sources, string rootDir, string sourcesExt, string outputDir, string outputExt) {
      Context = ctx;
      Sources = sources;
      RootDir = rootDir;
      SourcesExt = sourcesExt;
      OutputDir = outputDir;
      OutputExt = outputExt;
    }

    public void Command(string executablePath,
                        string args = null,
                        string cwd = null,
                        IDictionary<string, string> env = null) {
      Run(executablePath, args, cwd: cwd ?? BaseDir, env: env);
    }

    private BuildContext Context { get; }
    public string OutputDir { get; }
    public string OutputExt { get; }
    public ImmutableArray<string> Sources { get; }
    public string RootDir { get; }
    public string SourcesExt { get; }
    public TextWriter Stdout => Context.Stdout;
    public Stopwatch BuildStopwatch => Context.BuildStopwatch;
    public int ThisTaskNumber => Context.ThisTaskNumber;
    public int TotalTasks => Context.TotalTasks;
    public string BaseDir => Context.BaseDir;
  }
}