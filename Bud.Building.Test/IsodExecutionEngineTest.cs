using System;
using System.Collections.Immutable;
using System.IO;
using Moq;
using NUnit.Framework;

namespace Bud {
  public class IsodExecutionEngineTest {
    [Test]
    public void TestExecute_Task() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = FileGenerator("createFoo", "foo", "42").Object;

        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[0]);
      }
    }

    [Test]
    public void TestExecute_Dependencies() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = FileGenerator("createFoo", "foo", "42").Object;
        var barTask = FileGenerator("createBar", "bar", "9001", fooTask).Object;

        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, barTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[0]);
        FileAssert.AreEqual(tmpDir.CreateFile("9001"), buildResult.OutputFiles[1]);
      }
    }

    [Test]
    public void TestExecute_Once() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = FileGenerator("createFoo", "foo", "42");

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTaskMock.Object);
        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTaskMock.Object);

        fooTaskMock.Verify(f => f.Execute(It.IsAny<string>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_ClashingOutput() {
      using (var tmpDir = new TmpDir()) {
        var foo1TaskMock = FileGenerator("createFoo1", "foo", "1");
        var foo2TaskMock = FileGenerator("createFoo2", "foo", "2", foo1TaskMock.Object);

        var exception = Assert.Throws<Exception>(() => {
          IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, foo2TaskMock.Object);
        });

        Assert.That(exception.Message,
                    Contains.Substring("Tasks 'createFoo1' and 'createFoo2' are clashing. " +
                                       "They produced the same file 'foo'."));
      }
    }

    public static Mock<IBuildTask> FileGenerator(string taskName, string fileName, string fileContents, params IBuildTask[] dependencies) {
      var fileGeneratorMock = new Mock<IBuildTask>();

      fileGeneratorMock.SetupGet(f => f.Name).Returns(taskName);

      fileGeneratorMock.SetupGet(f => f.Dependencies).Returns(dependencies.ToImmutableArray);

      fileGeneratorMock.Setup(f => f.Execute(It.IsAny<string>()))
                       .Callback((string buildDir) => {
                         File.WriteAllText(Path.Combine(buildDir, fileName), fileContents);
                       });

      var signatureBytes = new Sha256Signer().Digest("MockFileGenerator")
                                             .Digest(fileName)
                                             .Digest(fileContents)
                                             .Finish()
                                             .Signature;

      fileGeneratorMock.SetupGet(f => f.Signature).Returns(HexUtils.ToHexStringFromBytes(signatureBytes));

      return fileGeneratorMock;
    }
  }
}