using System;
using System.IO;
using NUnit.Framework;
using static Bud.FileUtils;

namespace Bud {
  public class FilesUtilsTest {
    [Test]
    public void FindFilesByExt_in_a_non_existing_dir()
      => Assert.IsEmpty(FindFilesByExt("/foo/bar/does/not/exist", ""));

    [Test]
    public void FindFilesByExt_single_file() {
      using (var dir = new TmpDir()) {
        var barTxt = dir.CreateEmptyFile("foo", "bar.txt");
        Assert.AreEqual(new[] {barTxt}, FindFilesByExt(dir.CreateDir("foo"), ".txt"));
      }
    }

    [Test]
    public void FindFilesByExt_recursive() {
      using (var dir = new TmpDir()) {
        var barTxt = dir.CreateEmptyFile("foo", "baz", "bar.txt");
        Assert.AreEqual(new[] {barTxt}, FindFilesByExt(dir.CreateDir("foo"), ".txt"));
      }
    }

    [Test]
    public void FindFilesByExt_non_matching() {
      using (var dir = new TmpDir()) {
        var barTxt = dir.CreateEmptyFile("foo", "bartxt");
        Assert.IsEmpty(FindFilesByExt(dir.CreateDir("foo"), ".txt"));
      }
    }

    [Test]
    public void FindFilesByExt_non_recursive() {
      using (var dir = new TmpDir()) {
        var barTxt = dir.CreateEmptyFile("foo", "baz", "bartxt");
        Assert.IsEmpty(FindFilesByExt(dir.CreateDir("foo"), ".txt", SearchOption.TopDirectoryOnly));
      }
    }

    [Test]
    public void FindFilesRelative_single_file() {
      using (var dir = new TmpDir()) {
        dir.CreateEmptyFile("foo", "bar.txt");
        Assert.AreEqual(new[] {"bar.txt"}, FindFilesRelative(dir.CreateDir("foo")));
      }
    }

    [Test]
    public void FindFilesRelative_recursive() {
      using (var dir = new TmpDir()) {
        dir.CreateEmptyFile("foo", "baz", "bar.txt");
        Assert.AreEqual(new[] {"baz/bar.txt"}, FindFilesRelative(dir.CreateDir("foo")));
      }
    }

    [Test]
    public void FindFilesRelative_non_matching() {
      using (var dir = new TmpDir()) {
        dir.CreateEmptyFile("foo", "bar");
        Assert.IsEmpty(FindFilesRelative(dir.CreateDir("foo"), ".txt"));
      }
    }

    [Test]
    public void FindFilesRelative_non_recursive() {
      using (var dir = new TmpDir()) {
        dir.CreateEmptyFile("foo", "baz", "bartxt");
        Assert.IsEmpty(FindFilesRelative(dir.CreateDir("foo"), ".txt", SearchOption.TopDirectoryOnly));
      }
    }

    [Test]
    public void CopyTree_invalid_source_dir() {
      var exception = Assert.Throws<Exception>(() => CopyTree("/invalid/directory", "/moot"));
      Assert.AreEqual(exception.Message, "The directory '/invalid/directory' does not exist.");
    }

    [Test]
    public void CopyTree_creates_target_dir() {
      using (var tmpDir = new TmpDir()) {
        var sourceFileFoo = tmpDir.CreateFile("42", "src", "foo");
        CopyTree(tmpDir.CreatePath("src"), tmpDir.CreatePath("tgt"));
        FileAssert.AreEqual(sourceFileFoo, tmpDir.CreatePath("tgt", "foo"));
      }
    }

    [Test]
    public void CopyTree_target_dir_exists() {
      using (var tmpDir = new TmpDir()) {
        var sourceFileFoo = tmpDir.CreateFile("42", "src", "foo");
        CopyTree(tmpDir.CreatePath("src"), tmpDir.CreateDir("tgt"));
        FileAssert.AreEqual(sourceFileFoo, tmpDir.CreatePath("tgt", "foo"));
      }
    }

    [Test]
    public void CopyTree_creates_target_subdir() {
      using (var tmpDir = new TmpDir()) {
        var sourceFileFoo = tmpDir.CreateFile("42", "src", "bar", "foo");
        CopyTree(tmpDir.CreatePath("src"), tmpDir.CreatePath("tgt"));
        FileAssert.AreEqual(sourceFileFoo, tmpDir.CreatePath("tgt", "bar", "foo"));
      }
    }

    [Test]
    public void CopyTree_overwrite_existing() {
      using (var tmpDir = new TmpDir()) {
        var sourceFileFoo = tmpDir.CreateFile("42", "src", "bar", "foo");
        var targetFile = tmpDir.CreateFile("1", "tgt", "bar", "foo");
        CopyTree(tmpDir.CreatePath("src"), tmpDir.CreatePath("tgt"));
        FileAssert.AreEqual(sourceFileFoo, targetFile);
      }
    }
  }
}