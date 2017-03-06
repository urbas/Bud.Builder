using System.IO;
using NUnit.Framework;

namespace Bud {
  public class IsodExecutionEngineTest {
    [Test]
    public void TestExecuteSingleTask() {
      using (var tmpDir = new TmpDir()) {
        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, new GenerateFooFile());
        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[0]);
      }
    }
  }

  public class GenerateFooFile : IBuildTask {
    public void Execute(string buildDir) => File.WriteAllText(Path.Combine(buildDir, "foo"), "42");
  }
}