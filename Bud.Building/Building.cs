using System.Collections.Generic;
using System.IO;

namespace Bud {
  /// <summary>
  ///   Provides static functions for defining and executing builds.
  /// </summary>
  public static class Building {
    /// <summary>
    ///   Creates a build task that will invoke the given action.
    /// </summary>
    /// <param name="action">the action to invoke in this build task.</param>
    /// <param name="name">
    ///   the name of this build task (this name will be used in the build output and build logs).
    /// </param>
    /// <param name="dependsOn">other build tasks that should be invoked before this build task.</param>
    /// <returns>the build task object.</returns>
    public static BuildActionTask Build(BuildAction action, string name = null, IEnumerable<BuildTask> dependsOn = null)
      => new BuildActionTask(action, name, dependsOn);

    /// <summary>
    ///   Creates a build task where multiple sources are built into multiple output files.
    /// </summary>
    /// <param name="command">this function performs the actual build.</param>
    /// <param name="sourceDir">the directory in which to search for source files.</param>
    /// <param name="sourceExt">the extension of source files.</param>
    /// <param name="outputDir">the directory where output files will be placed.</param>
    /// <param name="outputExt">the extension of output files.</param>
    /// <param name="signature">see <see cref="BuildGlobToExtTask.Signature"/></param>
    /// <param name="dependsOn">other build tasks that this task depends on.</param>
    /// <returns>a build task that can be executed or can be used as a dependency of another task.</returns>
    public static BuildGlobToExtTask Build(BuildGlobToExtCommand command,
                                           string sourceDir,
                                           string sourceExt,
                                           string outputDir,
                                           string outputExt,
                                           string signature = null,
                                           IEnumerable<BuildTask> dependsOn = null)
      => new BuildGlobToExtTask(command, sourceDir, sourceExt, outputDir, outputExt, signature, dependsOn);

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="task">the task that describes the build.</param>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="baseDir">the directory in which the build is to be executed. By default the current working
    /// directory is used.</param>
    /// <param name="metaDir">the directory where meta information about the build system is stored. By default
    /// the subdirectory `.bud` in the base directory is used.</param>
    public static void RunBuild(BuildTask task,
                                TextWriter stdout = null,
                                string baseDir = null,
                                string metaDir = null)
      => RunBuild(new[] {task}, stdout, baseDir, metaDir);

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="tasks">the tasks that describe the build.</param>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="baseDir">the directory in which the build is to be executed. By default the current working
    /// directory is used.</param>
    /// <param name="metaDir">the directory where meta information about the build system is stored. By default
    /// the subdirectory `.bud` in the base directory is used.</param>
    public static void RunBuild(IEnumerable<BuildTask> tasks,
                                TextWriter stdout = null,
                                string baseDir = null,
                                string metaDir = null)
      => BuildExecution.RunBuild(tasks, stdout, baseDir, metaDir);
  }
}