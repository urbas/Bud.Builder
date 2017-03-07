using System.Collections.Immutable;
using System.IO;
using NUnit.Framework;

namespace Bud {
  public class IsodExecutionEngineTest {
    private static readonly GenerateFile FooTask = new GenerateFile("foo", "42");
    private static readonly GenerateFile BarTask = new GenerateFile("bar", "9001", FooTask);

    [Test]
    public void TestExecute_Task() {
      using (var tmpDir = new TmpDir()) {
        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, FooTask);
        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[0]);
      }
    }

    [Test]
    public void TestExecute_Dependencies() {
      using (var tmpDir = new TmpDir()) {
        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, BarTask);
        FileAssert.AreEqual(tmpDir.CreateFile("9001"), buildResult.OutputFiles[0]);
        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[1]);
      }
    }
  }

  internal class GenerateFile : IBuildTask {
    private readonly string fileName;
    private readonly string fileContents;
    private readonly ImmutableArray<IBuildTask> dependencies;

    public GenerateFile(string fileName, string fileContents, params IBuildTask[] dependencies) {
      this.fileName = fileName;
      this.fileContents = fileContents;
      this.dependencies = dependencies.ToImmutableArray();
    }

    public void Execute(string buildDir) => File.WriteAllText(Path.Combine(buildDir, fileName), fileContents);
    public ImmutableArray<IBuildTask> Dependencies => dependencies;
  }
}