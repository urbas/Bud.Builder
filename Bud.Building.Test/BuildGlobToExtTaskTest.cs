using System;
using System.IO;
using Bud.BuildingTesterApp.Options;
using NUnit.Framework;
using static Bud.Building;
using static Bud.Exec;
using static Bud.TesterAppPath;

namespace Bud {
  public class BuildGlobToExtTaskTest {
    [Test]
    public void Build_produces_glob_to_ext_task() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        dir.CreateFile("  bar  ", "src", "subdir", "bar.txt");

        var task = Build(command: ctx => ctx.Command(TesterApp, $"--rootDir {Arg(ctx.SourceDir)} " +
                                                                $"--outDir {Arg(ctx.OutputDir)} " +
                                                                $"--outExt .txt.nospace " +
                                                                $"{Args(ctx.Sources)}"),
                         sourceDir: "src",
                         sourceExt: ".txt",
                         outputDir: "build",
                         outputExt: ".txt.nospace");

        RunBuild(task, stdout: new StringWriter(), baseDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo", "foo.expected"),
                            dir.CreatePath("build", "foo.txt.nospace"));

        FileAssert.AreEqual(dir.CreateFile("bar", "bar.expected"),
                            dir.CreatePath("build", "subdir", "bar.txt.nospace"));
      }
    }

    [Test]
    public void Command_not_reinvoked() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        var outputFile = dir.CreatePath("build", "foo.txt.nospace");

        RunBuild(TrimTxtFiles(), stdout: new StringWriter(), baseDir: dir.Path);
        var modificationTime = File.GetLastWriteTimeUtc(outputFile);
        RunBuild(TrimTxtFiles(), stdout: new StringWriter(), baseDir: dir.Path);

        Assert.AreEqual(modificationTime, File.GetLastWriteTimeUtc(outputFile));
      }
    }

    [Test]
    public void Command_reinvoked_on_change() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");

        RunBuild(TrimTxtFiles(), stdout: new StringWriter(), baseDir: dir.Path);
        dir.CreateFile("  foo2  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(), stdout: new StringWriter(), baseDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo2", "foo.expected"),
                            dir.CreatePath("build", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Extraneous_files_deleted() {
      using (var dir = new TmpDir()) {
        var srcFile = dir.CreateFile("  foo  ", "src", "foo.txt");

        RunBuild(TrimTxtFiles(), stdout: new StringWriter(), baseDir: dir.Path);
        File.Delete(srcFile);
        RunBuild(TrimTxtFiles(), stdout: new StringWriter(), baseDir: dir.Path);

        FileAssert.DoesNotExist(dir.CreatePath("build", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Build_tasks_with_same_sources_do_not_interfere() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");

        RunBuild(TrimTxtFiles(outputDir: "build2"), stdout: new StringWriter(), baseDir: dir.Path);

        dir.CreateFile("  foo2  ", "src", "foo.txt");

        RunBuild(TrimTxtFiles(outputDir: "build1"), stdout: new StringWriter(), baseDir: dir.Path);
        RunBuild(TrimTxtFiles(outputDir: "build2"), stdout: new StringWriter(), baseDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo2", "foo.expected"),
                            dir.CreatePath("build2", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Build_tasks_rebuild_old_state() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(outputDir: "build"), stdout: new StringWriter(), baseDir: dir.Path);
        dir.CreateFile("  foo2  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(outputDir: "build"), stdout: new StringWriter(), baseDir: dir.Path);
        dir.CreateFile("  foo  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(outputDir: "build"), stdout: new StringWriter(), baseDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo", "foo.expected"),
                            dir.CreatePath("build", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Half_finished_failed_task_does_not_prevent_subsequent_builds() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(outputDir: "build"), stdout: new StringWriter(), baseDir: dir.Path);

        dir.CreateFile("  foo2  ", "src", "foo.txt");
        try {
          RunBuild(Build(ctx => {
                           TrimVerb.TrimTxtFiles(ctx.SourceDir, ctx.Sources, ctx.OutputDir, ctx.OutputExt);
                           throw new Exception("failure");
                         },
                         sourceDir: "src",
                         sourceExt: "txt",
                         outputDir: "build",
                         outputExt: ".txt.nospace"),
                   stdout: new StringWriter(),
                   baseDir: dir.Path);
        } catch (Exception) {
          // ignored
        }

        dir.CreateFile("  foo  ", "src", "foo.txt");
        RunBuild(TrimTxtFiles(outputDir: "build"), stdout: new StringWriter(), baseDir: dir.Path);

        FileAssert.AreEqual(dir.CreateFile("foo", "foo.expected"), dir.CreatePath("build", "foo.txt.nospace"));
      }
    }

    [Test]
    public void Throw_when_given_conflicting_build_tasks() {
      using (var dir = new TmpDir()) {
        var exception = Assert.Throws<AggregateException>(() => RunBuild(new[] {
                                                                           TrimTxtFiles(outputDir: "build"),
                                                                           TrimTxtFiles(outputDir: "build")
                                                                         },
                                                                         stdout: new StringWriter(),
                                                                         baseDir: dir.Path));
        Assert.AreEqual("Clashing build specification. Found duplicate tasks: " +
                        "'src/**/*.txt -> build/**/*.txt.nospace' and 'src/**/*.txt -> build/**/*.txt.nospace'.",
                        exception.InnerExceptions[0].Message);
      }
    }

    [Test]
    public void No_accidentally_conflicting_signatures() {
      using (var dir = new TmpDir()) {
        Assert.DoesNotThrow(() => RunBuild(new[] {
                                             TrimTxtFiles(outputDir: "build", outputExt: ".out"),
                                             TrimTxtFiles(outputDir: "bui", outputExt: "ld.out")
                                           },
                                           stdout: new StringWriter(),
                                           baseDir: dir.Path));
      }
    }

    [Test]
    [Ignore("TODO")]
    public void Throw_when_two_build_tasks_build_the_same_file() {
      using (var dir = new TmpDir()) {
        dir.CreateFile(" foo ", "src1", "foo.txt");
        dir.CreateFile(" bar ", "src2", "foo.txt");

        TestDelegate testDelegate = () => {
          RunBuild(new[] {
                     TrimTxtFiles(sourceDir: "src1", outputDir: "build"),
                     TrimTxtFiles(sourceDir: "src2", outputDir: "build")
                   },
                   stdout: new StringWriter(),
                   baseDir: dir.Path);
        };

        var clashingFile = dir.CreatePath("build", "foo.txt.nospace");


        var exception = Assert.Throws<Exception>(testDelegate);
        Assert.AreEqual($"Two builds produce the file '{clashingFile}'. " +
                        $"Build 'src1/**/*.txt -> build/**/*.txt.nospace' and 'src2/**/*.txt.nospace'.",
                        exception.Message);
      }
    }

    [Test]
    public void Allow_tasks_with_same_build_dirs_but_different_output_exts() {
      using (var dir = new TmpDir()) {
        dir.CreateFile("  foo  ", "src", "foo.txt");
        var expectedOutput = dir.CreateFile("foo", "foo.expected");

        RunBuild(new[] {TrimTxtFiles(outputExt: ".nospace1"), TrimTxtFiles(outputExt: ".nospace2")},
                 stdout: new StringWriter(), baseDir: dir.Path);

        FileAssert.AreEqual(expectedOutput, dir.CreatePath("build", "foo.nospace1"));
        FileAssert.AreEqual(expectedOutput, dir.CreatePath("build", "foo.nospace2"));
      }
    }

    private static BuildGlobToExtTask TrimTxtFiles(string sourceDir = "src", string sourceExt = ".txt", string outputExt = ".txt.nospace", string outputDir = "build")
      => new BuildGlobToExtTask(command: ctx => TrimVerb.TrimTxtFiles(ctx.SourceDir, ctx.Sources, ctx.OutputDir,
                                                                      ctx.OutputExt),
                                sourceDir: sourceDir,
                                sourceExt: sourceExt,
                                outputDir: outputDir,
                                outputExt: outputExt);
  }
}