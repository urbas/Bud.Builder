using System;
using System.Collections.Generic;
using System.IO;
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

        var task = Build(command: ctx => ctx.Command(TesterApp, $"--rootDir {Arg(ctx.SourceDir)} --outDir {Arg(ctx.OutputDir)} {Args(ctx.Sources)}"),
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
      // TODO: Add a test where a task fails, which prevents the deletion of the old state, and then a fresh task
      // doesn't rebuild
    }

    [Test]
    [Ignore("TODO")]
    public void Throw_when_given_conflicting_build_tasks() {
      var exception = Assert.Throws<Exception>(() => RunBuild(new[] {
                                                                TrimTxtFiles(outputDir: "build"),
                                                                TrimTxtFiles(outputDir: "build")
                                                              }));
      Assert.That(exception.Message,
                  Contains.Substring("Invalid build specification.")
                          .And.Contains("Found duplicate tasks 'src/**/*.txt -> build/**/*.txt.nospace'."));
    }

    [Test]
    [Ignore("TODO")]
    public void Throw_when_one_build_task_builds_into_the_build_dir_of_another() {
      var exception = Assert.Throws<Exception>(() => RunBuild(new[] {
                                                                TrimTxtFiles(outputDir: "build"),
                                                                TrimTxtFiles(outputDir: "build/foo")
                                                              }));
      Assert.That(exception.Message,
                  Contains.Substring("Invalid build specification.")
                          .And.Contains("Found a task that produces files 'build/**/*.txt.nospace' and another that " +
                                        "produces files 'build/foo/**/*.txt.nospace'."));
    }

    [Test]
    [Ignore("TODO")]
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

    private static BuildGlobToExtTask TrimTxtFiles(string outputExt = ".txt.nospace", string outputDir = "build")
      => new BuildGlobToExtTask(command: ctx => TrimTxtFiles(ctx.SourceDir, ctx.Sources, ctx.OutputDir),
                                sourceDir: "src",
                                sourceExt: ".txt",
                                outputDir: outputDir,
                                outputExt: outputExt);

    private static void TrimTxtFiles(string rootDir, IEnumerable<string> sourceFiles, string outDir) {
      var rootDirUri = new Uri($"{rootDir}/");
      foreach (var sourceFile in sourceFiles) {
        var content = File.ReadAllText(sourceFile).Trim();
        var relativeUri = rootDirUri.MakeRelativeUri(new Uri(sourceFile));
        var combine = Path.Combine(outDir, $"{relativeUri}.nospace");
        Directory.CreateDirectory(Path.GetDirectoryName(combine));
        File.WriteAllText(combine, content);
      }
    }
  }
}