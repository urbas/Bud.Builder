using System;
using System.Threading;
using Moq;
using NUnit.Framework;

namespace Bud {
  public class IsodExecutionEngineTest {
    [Test]
    public void TestExecute() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.FileGenerator("createFoo", "foo", "42").Object;

        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[0]);
      }
    }

    [Test]
    public void TestExecute_Dependencies() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.FileGenerator("createFoo", "foo", "42").Object;
        var barTask = MockBuildTasks.FileGenerator("createBar", "bar", "9001", fooTask).Object;

        var buildResult = IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, barTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), buildResult.OutputFiles[0]);
        FileAssert.AreEqual(tmpDir.CreateFile("9001"), buildResult.OutputFiles[1]);
      }
    }

    [Test]
    public void TestExecute_Once() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.FileGenerator("createFoo", "foo", "42");

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTaskMock.Object);
        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, fooTaskMock.Object);

        fooTaskMock.Verify(f => f.Execute(It.IsAny<string>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_ClashingOutput() {
      using (var tmpDir = new TmpDir()) {
        var foo1TaskMock = MockBuildTasks.FileGenerator("createFoo1", "foo", "1");
        var foo2TaskMock = MockBuildTasks.FileGenerator("createFoo2", "foo", "2", foo1TaskMock.Object);

        var exception = Assert.Throws<Exception>(() => {
          IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, foo2TaskMock.Object);
        });

        Assert.That(exception.Message,
                    Contains.Substring("Tasks 'createFoo1' and 'createFoo2' are clashing. " +
                                       "They produced the same file 'foo'."));
      }
    }

    [Test]
    public void TestExecute_DependenciesChange() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.FileGenerator("createFoo", "foo", "42");
        var barTaskMock = MockBuildTasks.FileGenerator("createBar", "bar", "9001", fooTaskMock.Object);
        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, barTaskMock.Object);

        var changedFooTaskMock = MockBuildTasks.FileGenerator("createFoo", "foo", "changed");
        var barTaskMock2 = MockBuildTasks.FileGenerator("createBar", "bar", "9001", changedFooTaskMock.Object);
        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, barTaskMock2.Object);

        barTaskMock2.Verify(f => f.Execute(It.IsAny<string>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_DuplicateDependenciesExecutedOnce() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.FileGenerator("createFoo", "foo", "42");
        var barTaskMock = MockBuildTasks.FileGenerator("createBar", "bar", "9001",
                                                       fooTaskMock.Object, fooTaskMock.Object);

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, barTaskMock.Object);

        fooTaskMock.Verify(f => f.Execute(It.IsAny<string>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_ParallelDependencies() {
      using (var tmpDir = new TmpDir()) {
        var countdownLatch = new CountdownEvent(2);

        var buildTask1Mock = MockBuildTasks.ActionBuildTask("latch1", () => {
          countdownLatch.Signal();
          countdownLatch.Wait();
        });

        var buildTask2Mock = MockBuildTasks.ActionBuildTask("latch2", () => {
          countdownLatch.Signal();
          countdownLatch.Wait();
        });

        var rootTaskMock = MockBuildTasks.NoOpBuildTask("rootTask", buildTask1Mock.Object, buildTask2Mock.Object);

        IsodExecutionEngine.Execute(tmpDir.Path, tmpDir.Path, rootTaskMock.Object);
      }
    }
  }
}