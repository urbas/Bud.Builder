using System;
using static Bud.Building;

namespace Bud {
  public class BuildingTest {
    public void RunBuild_executes_the_task() {
      var removeSpaces = Build(command: ctx => Console.WriteLine("Hello world!"),
                               sources: "src/txt/**/*.txt",
                               outputDir: "build/",
                               outputExt: ".nospace");
      // TODO: Test that files are created.
      // TODO: Test that output logs look as they should.
      // TODO: Test that the exit code corresponds to the result of the build.
      RunBuild(Console.Out, removeSpaces);
    }
  }
}