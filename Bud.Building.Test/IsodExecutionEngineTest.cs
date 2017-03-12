using System;
using System.IO;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;

namespace Bud {
  public class IsodExecutionEngineTest {
    [Test]
    public void TestExecute() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.GenerateFile("createFoo", "foo", "42").Object;

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), tmpDir.CreatePath("out", "foo"));
      }
    }

    [Test]
    public void TestExecute_Dependencies() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.GenerateFile("createFoo", "foo", "42").Object;
        var barTask = MockBuildTasks.GenerateFile("createBar", "bar", "9001", fooTask).Object;

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), tmpDir.CreatePath("out", "foo"));
        FileAssert.AreEqual(tmpDir.CreateFile("9001"), tmpDir.CreatePath("out", "bar"));
      }
    }

    [Test]
    public void TestExecute_Once() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTaskMock.Object);
        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTaskMock.Object);

        fooTaskMock.Verify(f => f.Execute(It.IsAny<BuildTaskContext>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_ClashingOutput() {
      using (var tmpDir = new TmpDir()) {
        var foo1TaskMock = MockBuildTasks.GenerateFile("createFoo1", "foo", "1");
        var foo2TaskMock = MockBuildTasks.GenerateFile("createFoo2", "foo", "2", foo1TaskMock.Object);

        var exception = Assert.Throws<Exception>(() => {
          IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), foo2TaskMock.Object);
        });

        Assert.That(exception.Message,
                    Contains.Substring("Tasks 'createFoo1' and 'createFoo2' are clashing. " +
                                       "They produced the same file 'foo'."));
      }
    }

    [Test]
    public void TestExecute_DependenciesChange() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");
        var barTaskMock = MockBuildTasks.GenerateFile("createBar", "bar", "9001", fooTaskMock.Object);
        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock.Object);

        var changedFooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "changed");
        var barTaskMock2 = MockBuildTasks.GenerateFile("createBar", "bar", "9001", changedFooTaskMock.Object);
        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock2.Object);

        barTaskMock2.Verify(f => f.Execute(It.IsAny<BuildTaskContext>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_DuplicateDependenciesExecutedOnce() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");
        var barTaskMock = MockBuildTasks.GenerateFile("createBar", "bar", "9001",
                                                      fooTaskMock.Object, fooTaskMock.Object);

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock.Object);

        fooTaskMock.Verify(f => f.Execute(It.IsAny<BuildTaskContext>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_ParallelExecution() {
      using (var tmpDir = new TmpDir()) {
        var countdownLatch = new CountdownEvent(2);

        var buildTask1Mock = MockBuildTasks.Action("latch1", () => {
          countdownLatch.Signal();
          countdownLatch.Wait();
        });

        var buildTask2Mock = MockBuildTasks.Action("latch2", () => {
          countdownLatch.Signal();
          countdownLatch.Wait();
        });

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), buildTask1Mock.Object, buildTask2Mock.Object);
      }
    }

    [Test]
    public void TestExecute_ClashingSignature() {
      using (var tmpDir = new TmpDir()) {
        var task1Mock = MockBuildTasks.NoOp("task1").WithSignature("foo");
        var task2Mock = MockBuildTasks.NoOp("task2", task1Mock.Object).WithSignature("foo");

        var exception = Assert.Throws<Exception>(() => {
          IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), task2Mock.Object);
        });

        Assert.That(exception.Message,
                    Contains.Substring("Tasks 'task1' and 'task2' are clashing. " +
                                       "They have the same signature 'foo'."));
      }
    }

    [Test]
    public void TestExecute_Source() {
      using (var tmpDir = new TmpDir()) {
        var sourceDir = tmpDir.CreateDir("src");
        var outputDir = tmpDir.CreatePath("out");
        var metaDir = tmpDir.CreatePath(".bud");
        var sourceFile = tmpDir.CreateFile("42", "src", "foo");
        var task1Mock = MockBuildTasks.CopySourceFile("task1", "foo", "bar");

        IsodExecutionEngine.Execute(sourceDir, outputDir, metaDir, task1Mock.Object);

        FileAssert.AreEqual(sourceFile, Path.Combine(outputDir, "bar"));
      }
    }

    [Test]
    public void TestExecute_StressTest() {
      for (int i = 0; i < 100; i++) {
        using (var tmpDir = new TmpDir()) {
          var sourceDir = tmpDir.CreateDir("src");
          var outputDir = tmpDir.CreatePath("out");
          var metaDir = tmpDir.CreatePath(".bud");

          var generatedFiles = Enumerable.Range(0, 100).Select(idx => $"file_{idx}").ToList();
          var fileGenerators = generatedFiles.Select(file => MockBuildTasks.GenerateFile(file, file, file).Object);

          IsodExecutionEngine.Execute(sourceDir, outputDir, metaDir, fileGenerators);

          foreach (var generatedFile in generatedFiles) {
            FileAssert.AreEqual(tmpDir.CreateFile(generatedFile), Path.Combine(outputDir, generatedFile));
          }
        }
      }
    }
  }
}