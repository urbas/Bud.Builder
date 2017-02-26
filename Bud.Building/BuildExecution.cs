using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bud {
  internal static class BuildExecution {
    public const string BuildMetaDirName = ".bud";
    public const string TaskSignaturesDirName = "task_signatures";

    internal static void RunBuild(IEnumerable<BuildTask> tasks,
                                  TextWriter stdout,
                                  string baseDir,
                                  string metaDir) {
      var buildTasks = tasks as IList<BuildTask> ?? tasks.ToList();
      baseDir = baseDir ?? Directory.GetCurrentDirectory();
      metaDir = metaDir ?? Path.Combine(baseDir, BuildMetaDirName);
      new ExecutionEngine(stdout, buildTasks, baseDir, metaDir).ExecuteBuild();
    }

  }
}