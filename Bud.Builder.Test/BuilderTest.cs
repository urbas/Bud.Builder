using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;

namespace Bud {
  public class BuilderTest {
    [Test]
    public void TestExecute_places_output_into_OutputDir() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.GenerateFile("createFoo", "foo", "42").Object;
        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTask);
        FileAssert.AreEqual(tmpDir.CreateFile("42"), tmpDir.CreatePath("out", "foo"));
      }
    }

    [Test]
    public void TestExecute_executes_dependencies() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.GenerateFile("createFoo", "foo", "42").Object;
        var barTask = MockBuildTasks.GenerateFile("createBar", "bar", "9001", fooTask).Object;

        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTask);

        FileAssert.AreEqual(tmpDir.CreateFile("42"), tmpDir.CreatePath("out", "foo"));
        FileAssert.AreEqual(tmpDir.CreateFile("9001"), tmpDir.CreatePath("out", "bar"));
      }
    }

    [Test]
    public void TestExecute_dependency_results_reference_build_tasks() {
      using (var tmpDir = new TmpDir()) {
        var fooTask = MockBuildTasks.NoOp("foo").Object;
        var barTask = MockBuildTasks.NoOp("bar", fooTask)
                                    .WithExecuteAction((sourceDir, outputDir, dependencyResults) => {
                                      Assert.AreEqual(new []{fooTask}, 
                                                      dependencyResults.Select(result => result.BuildTask));
                                    }).Object;

        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTask);
      }
    }

    [Test]
    public void TestExecute_executes_the_same_tasks_once() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");

        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTaskMock.Object);
        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fooTaskMock.Object);

        VerifyExecutedOnce(fooTaskMock);
      }
    }

    [Test]
    public void TestExecute_throws_when_two_tasks_produce_file_with_same_name() {
      using (var tmpDir = new TmpDir()) {
        var foo1TaskMock = MockBuildTasks.GenerateFile("createFoo1", "foo", "1");
        var foo2TaskMock = MockBuildTasks.GenerateFile("createFoo2", "foo", "2", foo1TaskMock.Object);

        var exception = Assert.Throws<Exception>(() => {
          Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), foo2TaskMock.Object);
        });

        Assert.That(exception.Message,
                    Contains.Substring("Tasks 'createFoo1' and 'createFoo2' are clashing. " +
                                       "They produced the same file 'foo'."));
      }
    }

    [Test]
    public void TestExecute_rebuilds_task_when_signature_of_dependency_changes() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");
        var barTaskMock = MockBuildTasks.GenerateFile("createBar", "bar", "9001", fooTaskMock.Object);
        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock.Object);

        var changedFooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "changed");
        var barTaskMock2 = MockBuildTasks.GenerateFile("createBar", "bar", "9001", changedFooTaskMock.Object);
        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock2.Object);

        VerifyExecutedOnce(barTaskMock2);
      }
    }

    [Test]
    public void TestExecute_same_dependencies_are_executed_once() {
      using (var tmpDir = new TmpDir()) {
        var fooTaskMock = MockBuildTasks.GenerateFile("createFoo", "foo", "42");
        var barTaskMock = MockBuildTasks.GenerateFile("createBar", "bar", "9001",
                                                      fooTaskMock.Object, fooTaskMock.Object);

        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), barTaskMock.Object);

        VerifyExecutedOnce(fooTaskMock);
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

        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), buildTask1Mock.Object,
                        buildTask2Mock.Object);
      }
    }

    [Test]
    public void TestExecute_throws_when_two_tasks_have_the_same_signature() {
      using (var tmpDir = new TmpDir()) {
        var task1Mock = MockBuildTasks.NoOp("task1").WithSignature("foo");
        var task2Mock = MockBuildTasks.NoOp("task2", task1Mock.Object).WithSignature("foo");

        var exception = Assert.Throws<BuildTaskClashException>(() => {
          Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), task2Mock.Object);
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

        Builder.Execute(sourceDir, outputDir, metaDir, task1Mock.Object);

        FileAssert.AreEqual(sourceFile, Path.Combine(outputDir, "bar"));
      }
    }

    [Test]
    public void TestExecute_cleans_unfinished_directories_before_starting_the_build() {
      using (var tmpDir = new TmpDir()) {
        var partialTask = MockBuildTasks.NoOp("task1")
                                        .WithExecuteAction((sourceDir, outputDir, deps) => {
                                          File.WriteAllText(Path.Combine(outputDir, "foo"), "42");
                                          throw new Exception("Test exception");
                                        });
        try {
          Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), partialTask.Object);
        } catch (Exception) {
          // ignored
        }
        var fullTask = MockBuildTasks.NoOp("task1")
                                     .WithExecuteAction((sourceDir, outputDir, deps) => 
                                                          File.WriteAllText(Path.Combine(outputDir, "bar"), "9001"));
        Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"), tmpDir.CreateDir(".bud"), fullTask.Object);

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

          Builder.Execute(sourceDir, outputDir, metaDir, fileGenerators);

          foreach (var generatedFile in generatedFiles) {
            FileAssert.AreEqual(tmpDir.CreateFile(generatedFile), Path.Combine(outputDir, generatedFile));
          }
        }
      }
    }

    [Test]
    public void TestExecute_name_clash_throws() {
      using (var tmpDir = new TmpDir()) {
        var fooTask1 = MockBuildTasks.NoOp("foo").WithSignature("1").Object;
        var fooTask2 = MockBuildTasks.NoOp("foo").WithSignature("2").Object;
        var exception = Assert.Throws<Exception>(() => Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"),
                                                                       tmpDir.CreateDir(".bud"), fooTask1, fooTask2));
        Assert.AreEqual("Detected multiple tasks with the name 'foo'. Tasks must have unique names.",
                        exception.Message);
      }
    }

    [Test]
    public void TestExecute_detects_cycles() {
      using (var tmpDir = new TmpDir()) {
        var fooTask1 = MockBuildTasks.NoOp("foo1").WithSignature("1");
        var fooTask2 = MockBuildTasks.NoOp("foo2").WithSignature("2").WithDependencies(new []{fooTask1.Object});
        fooTask1.WithDependencies(new[] {fooTask2.Object});
        var exception = Assert.Throws<Exception>(() => Builder.Execute(tmpDir.Path, tmpDir.CreateDir("out"),
                                                                       tmpDir.CreateDir(".bud"), fooTask1.Object,
                                                                       fooTask2.Object));
        Assert.AreEqual("Detected a dependency cycle: 'foo1 depends on foo2 depends on foo1'.",
                        exception.Message);
      }
    }

    private static void VerifyExecutedOnce(Mock<IBuildTask> fooTaskMock)
      => fooTaskMock.Verify(f => f.Execute(It.IsAny<string>(),
                                           It.IsAny<string>(),
                                           It.IsAny<ImmutableArray<BuildTaskResult>>()),
                            Times.Once);
  }
}