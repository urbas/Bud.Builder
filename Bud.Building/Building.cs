using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bud {
  /// <summary>
  ///   Provides static functions for defining and executing builds.
  /// </summary>
  public static class Building {
    /// <summary>
    ///   The default name of the directory where the build execution engine will put intermediate build artifacts and
    ///   its meta data.
    /// </summary>
    public const string MetaDirName = ".bud";

    /// <summary>
    ///   The default name of the directory where the final artifacts of the build will be placed.
    /// </summary>
    public const string OutputDirName = "output";

    /// <summary>
    ///   Creates a build task where multiple sources are built into multiple output files.
    /// </summary>
    /// <param name="command">this function performs the actual build.</param>
    /// <param name="sourceDir">the directory in which to search for source files.</param>
    /// <param name="sourceExt">the extension of source files.</param>
    /// <param name="outputDir">the directory where output files will be placed.</param>
    /// <param name="outputExt">the extension of output files.</param>
    /// <param name="signature">see <see cref="GlobBuildTask.Salt"/></param>
    /// <param name="dependsOn">other build tasks that this task depends on.</param>
    /// <returns>a build task that can be executed or can be used as a dependency of another task.</returns>
    public static GlobBuildTask Build(GlobBuildCommand command,
                                      string sourceDir,
                                      string sourceExt,
                                      string outputDir,
                                      string outputExt,
                                      string signature = null,
                                      IEnumerable<IBuildTask> dependsOn = null)
      => new GlobBuildTask(command, sourceDir, sourceExt, outputDir, outputExt, signature, dependsOn);

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="task">the task that describes the build.</param>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="baseDir">
    ///   the directory in which the build is to be executed. By default the current working
    ///   directory is used.
    /// </param>
    /// <param name="outputDir">
    ///   the directory where the final output of the build should be placed. By default the
    ///   sudirectory <see cref="OutputDirName"/> of the <paramref name="baseDir"/> is used.
    /// </param>
    /// <param name="metaDir">
    ///   the directory where meta information about the build system is stored.  By default the
    ///   sudirectory <see cref="MetaDirName"/> of the <paramref name="baseDir"/> is used.
    /// </param>
    public static void RunBuild(IBuildTask task,
                                TextWriter stdout = null,
                                string baseDir = null,
                                string outputDir = null,
                                string metaDir = null)
      => RunBuild(new[] {task}, stdout, baseDir, outputDir, metaDir);

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="tasks">the tasks that describe the build.</param>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="baseDir">
    ///   the directory in which the build is to be executed. By default the current working
    ///   directory is used.
    /// </param>
    /// <param name="outputDir">
    ///   the directory where the final output of the build should be placed. By default the
    ///   sudirectory <see cref="OutputDirName"/> of the <paramref name="baseDir"/> is used.
    /// </param>
    /// <param name="metaDir">
    ///   the directory where meta information about the build system is stored.  By default the
    ///   sudirectory <see cref="MetaDirName"/> of the <paramref name="baseDir"/> is used.
    /// </param>
    public static void RunBuild(IEnumerable<IBuildTask> tasks,
                                TextWriter stdout = null,
                                string baseDir = null,
                                string outputDir = null,
                                string metaDir = null) {
      var buildTasks = tasks as IList<IBuildTask> ?? tasks.ToList();
      baseDir = baseDir ?? Directory.GetCurrentDirectory();
      metaDir = metaDir ?? Path.Combine(baseDir, MetaDirName);
      outputDir = outputDir ?? Path.Combine(baseDir, "build");
      IsodExecutionEngine.Execute(baseDir, outputDir, metaDir, buildTasks);
    }
  }
}