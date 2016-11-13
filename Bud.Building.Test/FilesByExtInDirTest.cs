using System.IO;
using NUnit.Framework;

namespace Bud {
  public class FilesUtilsTest {
    [Test]
    public void Find_in_a_non_existing_dir()
      => Assert.IsEmpty(FilesUtils.Find("/foo/bar/does/not/exist", ""));

    [Test]
    public void Find_single_file() {
      using (var dir = new TmpDir()) {
        var barTxt = dir.CreateEmptyFile("foo", "bar.txt");
        Assert.AreEqual(new[] {barTxt}, FilesUtils.Find(dir.CreateDir("foo"), ".txt"));
      }
    }

    [Test]
    public void ToAbsDir_dir_null() => Assert.AreEqual("/foo/bar", FilesUtils.ToAbsDir(null, "/foo/bar"));

    [Test]
    public void ToAbsDir_dir_empty() => Assert.AreEqual("/foo/bar", FilesUtils.ToAbsDir(string.Empty, "/foo/bar"));

    [Test]
    public void ToAbsDir_relative()
      => Assert.AreEqual(Path.Combine("/foo/bar", "not/yet/abs"), FilesUtils.ToAbsDir("not/yet/abs", "/foo/bar"));

    [Test]
    public void ToAbsDir_already_abs()
      => Assert.AreEqual("/already/abs", FilesUtils.ToAbsDir("/already/abs", "/foo/bar"));
  }
}