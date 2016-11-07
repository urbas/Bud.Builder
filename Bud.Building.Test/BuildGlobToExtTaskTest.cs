using NUnit.Framework;

namespace Bud {
  public class BuildGlobToExtTaskTest {
    [Test]
    [Ignore("TODO: Not yet implemented.")]
    public void Build_produces_glob_to_ext_task() {
      var task = Building.Build(command:   ctx => ctx.Command("tsc.exe", $"--rootDir src --outDir {ctx.OutputDir} {Exec.Args(ctx.Sources)}"),
                                sources:   "src/**/*.ts",
                                outputDir: "build/js",
                                outputExt: ".js");
    }
  }
}