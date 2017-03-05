using Moq;

namespace Bud {
  public class IsodExecutionEngineTest {
    public void TestExecuteSingleTask() {
      using (var tmpDir = new TmpDir()) {
        var buildTaskA = new Mock<BuildTask>().Object;
        var buildDir = tmpDir.CreatePath("build");
        var buildResult = IsodExecutionEngine.Execute(baseDir: tmpDir.Path,
                                                      buildDir: buildDir,
                                                      buildTasks: new [] { buildTaskA });
      }
    }
  }
}