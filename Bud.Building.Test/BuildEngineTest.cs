using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;

namespace Bud {
  public class BuildEngineTest {
    [Test]
    public void TestExecute_places_output_into_OutputDir() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.GenerateFile("createFoo", "foo", "42").Object;
        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTask);
        FileAssert.AreEqual(tmpDir.CreateFile("42"), tmpDir.CreatePath("out", "foo"));
      }
    }

    [Test]
    public void TestExecute_executes_dependencies() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.GenerateFile("createFoo", "foo", "42").Object;
        var barTask = MockBuildTasks.GenerateFile("createBar", "bar", "9001", fooTask).Object;

        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), tmpDir.CreatePath("out", "foo"));
        FileAssert.AreEqual(tmpDir.CreateFile("9001"), tmpDir.CreatePath("out", "bar"));
      }
    }

    [Test]
    public void TestExecute_executes_the_same_tasks_once() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");

        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTaskMock.Object);
        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTaskMock.Object);

        fooTaskMock.Verify(f => f.Execute(It.IsAny<BuildTaskContext>(), It.IsAny<ImmutableArray<BuildTaskResult>>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_throws_when_two_tasks_produce_file_with_same_name() {
      using (var tmpDir = new TmpDir()) {
        var foo1TaskMock = MockBuildTasks.GenerateFile("createFoo1", "foo", "1");
        var foo2TaskMock = MockBuildTasks.GenerateFile("createFoo2", "foo", "2", foo1TaskMock.Object);

        var exception = Assert.Throws<Exception>(() => {
          BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), foo2TaskMock.Object);
        });

        Assert.That(exception.Message,
                    Contains.Substring("Tasks 'createFoo1' and 'createFoo2' are clashing. " +
                                       "They produced the same file 'foo'."));
      }
    }

    [Test]
    public void TestExecute_rebuilds_task_when_signature_changes() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");
        var barTaskMock = MockBuildTasks.GenerateFile("createBar", "bar", "9001", fooTaskMock.Object);
        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock.Object);

        var changedFooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "changed");
        var barTaskMock2 = MockBuildTasks.GenerateFile("createBar", "bar", "9001", changedFooTaskMock.Object);
        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock2.Object);

        barTaskMock2.Verify(f => f.Execute(It.IsAny<BuildTaskContext>(), It.IsAny<ImmutableArray<BuildTaskResult>>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_same_dependencies_are_executed_once() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");
        var barTaskMock = MockBuildTasks.GenerateFile("createBar", "bar", "9001",
                                                      fooTaskMock.Object, fooTaskMock.Object);

        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock.Object);

        fooTaskMock.Verify(f => f.Execute(It.IsAny<BuildTaskContext>(), It.IsAny<ImmutableArray<BuildTaskResult>>()), Times.Once);
      }
    }

    [Test]
    public void TestExecute_executes_tasks_in_parallel() {
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

        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), buildTask1Mock.Object, buildTask2Mock.Object);
      }
    }

    [Test]
    public void TestExecute_throws_when_two_tasks_have_the_same_signature() {
      using (var tmpDir = new TmpDir()) {
        var task1Mock = MockBuildTasks.NoOp("task1").WithSignature("foo");
        var task2Mock = MockBuildTasks.NoOp("task2", task1Mock.Object).WithSignature("foo");

        var exception = Assert.Throws<Exception>(() => {
          BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), task2Mock.Object);
        });

        Assert.That(exception.Message,
                    Contains.Substring("Tasks 'task1' and 'task2' are clashing. " +
                                       "They have the same signature 'foo'."));
      }
    }

    [Test]
    public void TestExecute_passes_the_source_dir_to_tasks() {
      using (var tmpDir = new TmpDir()) {
        var sourceDir = tmpDir.CreateDir("src");
        var outputDir = tmpDir.CreatePath("out");
        var metaDir = tmpDir.CreatePath(".bud");
        var sourceFile = tmpDir.CreateFile("42", "src", "foo");
        var task1Mock = MockBuildTasks.CopySourceFile("task1", "foo", "bar");

        BuildEngine.Execute(sourceDir, outputDir, metaDir, task1Mock.Object);

        FileAssert.AreEqual(sourceFile, Path.Combine(outputDir, "bar"));
      }
    }

    [Test]
    public void TestExecute_cleans_unfinished_directories_before_starting_the_build() {
      using (var tmpDir = new TmpDir()) {
        var partialTask = MockBuildTasks.NoOp("task1").WithExecuteAction((ctx, deps) => {
          File.WriteAllText(Path.Combine(ctx.OutputDir, "foo"), "42");
          throw new Exception("Test exception");
        });
        try {
          BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), partialTask.Object);
        } catch (Exception) {
          // ignored
        }
        var fullTask = MockBuildTasks.NoOp("task1").WithExecuteAction((ctx, deps) => {
          File.WriteAllText(Path.Combine(ctx.OutputDir, "bar"), "9001");
        });
        BuildEngine.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fullTask.Object);

        FileAssert.AreEqual(tmpDir.CreateFile("9001"), tmpDir.CreatePath("out", "bar"));
        FileAssert.DoesNotExist(tmpDir.CreatePath("out", "foo"));
      }
    }

    [Test]
    public void TestExecute_does_not_have_race_conditions() {
      for (int i = 0; i < 5; i++) {
        using (var tmpDir = new TmpDir()) {
          var sourceDir = tmpDir.CreateDir("src");
          var outputDir = tmpDir.CreatePath("out");
          var metaDir = tmpDir.CreatePath(".bud");

          var generatedFiles = Enumerable.Range(0, 10).Select(idx => $"file_{idx}").ToList();
          var fileGenerators = generatedFiles.Select(file => MockBuildTasks.GenerateFile(file, file, file).Object);

          BuildEngine.Execute(sourceDir, outputDir, metaDir, fileGenerators);

          foreach (var generatedFile in generatedFiles) {
            FileAssert.AreEqual(tmpDir.CreateFile(generatedFile), Path.Combine(outputDir, generatedFile));
          }
        }
      }
    }
  }
}