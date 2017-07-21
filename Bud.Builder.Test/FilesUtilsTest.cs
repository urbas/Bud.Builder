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
  }
}