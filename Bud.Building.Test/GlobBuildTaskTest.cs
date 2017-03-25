using System;
using System.Collections.Generic;
using System.IO;
using Bud.BuildingTesterApp.Options;
using NUnit.Framework;
using static Bud.Building;
using static Bud.Exec;
using static Bud.TesterAppPath;

namespace Bud {
  public class GlobBuildTaskTest {
    [Test]
    public void Build_produces_glob_to_ext_task() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        dir.CreateFile("  bar  ", "src", "subdir", "bar.txt");

        var task = Build(command: ctx => ctx.Command(TesterApp, $"--rootDir {Arg(ctx.SourceDir)} " +
                                                                $"--outDir {Arg(ctx.OutputDir)} " +
                                                                $"--outExt .txt.nospace " +
                                                                $"{Args(ctx.Sources)}"),
                         sourceExt: ".txt",
                         outputExt: ".txt.nospace", sourceDir: "src", outputDir: "");

        RunBuild(task, sourceDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo", "foo.expected"),
                            dir.CreatePath("output", "foo.txt.nospace"));

        FileAssert.AreEqual(dir.CreateFile("bar", "bar.expected"),
                            dir.CreatePath("output", "subdir", "bar.txt.nospace"));
      }
    }

    [Test]
    public void Command_not_reinvoked() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        var outputFile = dir.CreatePath("output", "foo.txt.nospace");

        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);
        var modificationTime = File.GetLastWriteTimeUtc(outputFile);
        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);

        Assert.AreEqual(modificationTime, File.GetLastWriteTimeUtc(outputFile));
      }
    }

    [Test]
    public void Command_reinvoked_on_change() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");

        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);
        dir.CreateFile("  foo2  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo2", "foo.expected"),
                            dir.CreatePath("output", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Extraneous_files_deleted() {
      using (var dir = new TmpDir()) {
        var srcFile = dir.CreateFile("  foo  ", "src", "foo.txt");

        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);
        File.Delete(srcFile);
        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);

        FileAssert.DoesNotExist(dir.CreatePath("output", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Build_tasks_with_same_sources_do_not_interfere() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");

        RunBuild(TrimTxtFiles(outputDir: "build2"), sourceDir: dir.Path);

        dir.CreateFile("  foo2  ", "src", "foo.txt");

        RunBuild(TrimTxtFiles(outputDir: "build1"), sourceDir: dir.Path);
        RunBuild(TrimTxtFiles(outputDir: "build2"), sourceDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo2", "foo.expected"),
                            dir.CreatePath("output", "build2", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Build_tasks_rebuild_old_state() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);
        dir.CreateFile("  foo2  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);
        dir.CreateFile("  foo  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo", "foo.expected"),
                            dir.CreatePath("output", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Half_finished_failed_task_does_not_prevent_subsequent_builds() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);

        dir.CreateFile("  foo2  ", "src", "foo.txt");
        try {
          RunBuild(Build(ctx => {
                           TrimVerb.TrimTxtFiles(ctx.SourceDir, ctx.Sources, ctx.OutputDir, ctx.OutputExt);
                           throw new Exception("failure");
                         },
                         sourceExt: "txt",
                         outputExt: ".txt.nospace", sourceDir: "src", outputDir: ""),
                   sourceDir: dir.Path);
        } catch (Exception) {
          // ignored
        }

        dir.CreateFile("  foo  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(), sourceDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo", "foo.expected"), dir.CreatePath("output", "foo.txt.nospace"));
      }
    }

    [Test]
    public void No_accidentally_conflicting_signatures() {
      using (var dir = new TmpDir()) {
        Assert.DoesNotThrow(() => RunBuild(new[] {
                                             TrimTxtFiles(outputDir: "build", outputExt: ".out"),
                                             TrimTxtFiles(outputDir: "bui", outputExt: "ld.out")
                                           },
                                           sourceDir: dir.Path));
      }
    }

    [Test]
    public void Tasks_with_different_source_dir_are_not_conflicting() {
      using (var dir = new TmpDir()) {
        Assert.DoesNotThrow(() => RunBuild(new[] {
                                             TrimTxtFiles(sourceDir: "foo"),
                                             TrimTxtFiles(sourceDir: "bar")
                                           },
                                           sourceDir: dir.Path));
      }
    }

    [Test]
    public void Tasks_with_different_source_ext_are_not_conflicting() {
      using (var dir = new TmpDir()) {
        Assert.DoesNotThrow(() => RunBuild(new[] {
                                             TrimTxtFiles(sourceExt: ".foo"),
                                             TrimTxtFiles(sourceExt: ".bar")
                                           },
                                           sourceDir: dir.Path));
      }
    }

    [Test]
    public void Files_produced_by_other_tasks_are_not_deleted() {
      using (var dir = new TmpDir()) {
        dir.CreateFile(" foo ", "src1", "foo.txt");
        dir.CreateFile(" bar ", "src2", "bar.txt");

        var fooTask = TrimTxtFiles(sourceDir: "src1");
        var barTask = TrimTxtFiles(sourceDir: "src2", dependsOn: new[] {fooTask});

        RunBuild(barTask, sourceDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo", "expected.foo"), dir.CreatePath("output", "foo.txt.nospace"));
        FileAssert.AreEqual(dir.CreateFile("bar", "expected.bar"), dir.CreatePath("output", "bar.txt.nospace"));
      }
    }

    [Test]
    public void Allow_tasks_with_same_build_dirs_but_different_output_exts() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        var expectedOutput = dir.CreateFile("foo", "foo.expected");

        RunBuild(new[] {TrimTxtFiles(outputExt: ".nospace1"), TrimTxtFiles(outputExt: ".nospace2")}, sourceDir: dir.Path);

        FileAssert.AreEqual(expectedOutput, dir.CreatePath("output", "foo.nospace1"));
        FileAssert.AreEqual(expectedOutput, dir.CreatePath("output", "foo.nospace2"));
      }
    }

    [Test]
    public void Build_README_example() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        dir.CreateFile("  bar  ", "src", "subdir", "bar.txt");

        var task = Build(command: ctx => ctx.Command(TesterApp, $"--rootDir {Arg(ctx.SourceDir)} " +
                                                                $"--outDir {Arg(ctx.OutputDir)} " +
                                                                $"--outExt .txt.nospace " +
                                                                $"{Args(ctx.Sources)}"),
                         sourceExt: ".txt",
                         outputExt: ".txt.nospace");

        RunBuild(task, sourceDir: dir.CreateDir("src"), outputDir: dir.CreateDir("out/nospace"));

        FileAssert.AreEqual(dir.CreateFile("foo"),
                            dir.CreatePath("out", "nospace", "foo.txt.nospace"));

        FileAssert.AreEqual(dir.CreateFile("bar"),
                            dir.CreatePath("out", "nospace", "subdir", "bar.txt.nospace"));
      }
    }

    private static GlobBuildTask TrimTxtFiles(string sourceDir = "src", string sourceExt = ".txt",
                                              string outputDir = "", string outputExt = ".txt.nospace",
                                              IEnumerable<IBuildTask> dependsOn = null)
      => new GlobBuildTask(command: ctx => TrimVerb.TrimTxtFiles(ctx.SourceDir, ctx.Sources, ctx.OutputDir,
                                                                 ctx.OutputExt),
                           sourceDir: sourceDir,
                           sourceExt: sourceExt,
                           outputDir: outputDir,
                           outputExt: outputExt,
                           dependencies: dependsOn);
  }
}