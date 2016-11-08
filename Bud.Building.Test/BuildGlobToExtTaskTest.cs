using System.IO;
using NUnit.Framework;
using static Bud.Building;
using static Bud.Exec;
using static Bud.TesterAppPath;

namespace Bud {
  public class BuildGlobToExtTaskTest {
    [Test]
    public void Build_produces_glob_to_ext_task() {
      using (var dir = new TmpDir()) {


        var task = Build(command: ctx => ctx.Command(TesterApp, $"trim --rootDir src --outDir {ctx.OutputDir} {Args(ctx.Sources)}"),
                         sources: "src/**/*.txt", outputDir: "build/js", outputExt: ".txt.nospace");

        RunBuild(task, stdout: new StringWriter(), baseDir: dir.Path);
      }
    }
  }
}