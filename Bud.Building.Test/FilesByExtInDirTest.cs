using NUnit.Framework;

namespace Bud {
  public class FilesByExtInDirTest {
    [Test]
    public void Find_in_a_non_existing_dir()
      => Assert.IsEmpty(new FilesByExtInDir("/foo/bar/does/not/exist", "").Find());

    [Test]
    public void Find_single_file() {
      using (var dir = new TmpDir()) {
        var barTxt = dir.CreateEmptyFile("foo", "bar.txt");
        Assert.AreEqual(new []{barTxt}, new FilesByExtInDir("foo", ".txt").Find(dir.Path));
      }
    }

    [Test]
    public void Find_without_dir() {
      using (var dir = new TmpDir()) {
        var barTxt = dir.CreateEmptyFile("bar.txt");
        Assert.AreEqual(new []{barTxt}, new FilesByExtInDir(ext: ".txt").Find(dir.Path));
      }
    }
  }
}