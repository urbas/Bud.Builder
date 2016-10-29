using System;
using System.Collections.Generic;
using System.IO;

namespace Bud {
  /// <summary>
  ///   Provides static functions for defining and executing builds.
  /// </summary>
  public static class Building {
    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="stdout">the writer to which to print all the build output.</param>
    /// <param name="buildTasks">the tasks that describe the build.</param>
    public static void RunBuild(TextWriter stdout, params BuildTask[] buildTasks) {}

    /// <summary>
    ///   Executes the build described by the build tasks.
    /// </summary>
    /// <param name="buildTasks">the tasks that describe the build.</param>
    public static void RunBuild(params BuildTask[] buildTasks) => RunBuild(Console.Out, buildTasks);

    public static BuildTask Build(BuildCommand command,
                                  string sources,
                                  string outputDir,
                                  string outputExt,
                                  IEnumerable<BuildTask> dependsOn = null) {
      throw new NotImplementedException();
    }
  }
}