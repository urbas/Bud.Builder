using System.Collections.Immutable;
using System.IO;
using Moq;
using NUnit.Framework;

namespace Bud {
  public class IsodExecutionEngineTest {
    [Test]
    public void TestExecute_Task() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = FileGenerator("foo", "42").Object;

        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[0]);
      }
    }

    [Test]
    public void TestExecute_Dependencies() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = FileGenerator("foo", "42").Object;
        var barTask = FileGenerator("bar", "9001", fooTask).Object;

        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, barTask);

        FileAssert.AreEqual(tmpDir.CreateFile("9001"), buildResult.OutputFiles[0]);
        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[1]);
      }
    }

    [Test]
    public void TestExecute_Once() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = FileGenerator("foo", "42");

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTaskMock.Object);
        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTaskMock.Object);

        fooTaskMock.Verify(f => f.Execute(It.IsAny<string>()), Times.Once);
      }
    }

    private static Mock<IBuildTask> FileGenerator(string fileName, string fileContents, params IBuildTask[] dependencies) {
      var fileGeneratorMock = new Mock<IBuildTask>();

      fileGeneratorMock.SetupGet(f => f.Dependencies).Returns(dependencies.ToImmutableArray);

      fileGeneratorMock.Setup(f => f.Execute(It.IsAny<string>())).Callback((string buildDir) => {
        File.WriteAllText(Path.Combine(buildDir, fileName), fileContents);
      });

      return fileGeneratorMock;
    }
  }
}