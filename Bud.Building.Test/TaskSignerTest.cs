using System;
using NUnit.Framework;
using static Bud.HexUtils;
using static NUnit.Framework.Assert;
using Contains = NUnit.Framework.Contains;

namespace Bud {
  public class TaskSignerTest {
    [Test]
    public void Hash_throws_at_the_beginning() {
      var exception = Throws<Exception>(() => {
                                          var _ = new TaskSigner().Hash;
                                        });
      That(exception.Message,
           Contains.Substring("The hash has not yet been calculated. Call 'Finish' to calculate the hash."));
    }

    [Test]
    public void Hash_of_empty_string()
      => AreEqual(ToBytesFromHexString("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"),
                  new TaskSigner().Finish().Hash);

    [Test]
    public void Hash_of_string_foo()
      => AreEqual(ToBytesFromHexString("2c26b46b68ffc68ff99b453c1d30413413422d706483bfa0f98a5e886266e7ae"),
                  new TaskSigner().Digest("foo").Finish().Hash);

    [Test]
    public void Hash_of_string_foo_with_small_buffer()
      => AreEqual(ToBytesFromHexString("e13f95bc2b118d3173ea119df73ef75ebbae9d595d9a236ec6aad3d392c5d405"),
                  new TaskSigner(new byte[4]).Digest("foo bar ™ glar har").Finish().Hash);

    [Test]
    public void Hash_throws_when_buffer_smaller_than_4() {
      var exception = Throws<ArgumentException>(() => new TaskSigner(new byte[3]));
      That(exception.Message,
           Contains.Substring("The buffer must have at least 4 bytes."));
    }

    [Test]
    public void Hash_of_file() {
      using (var dir = new TmpDir()) {
        var fooFile = dir.CreateFile("9001", "foo.txt");
        var barFile = dir.CreateFile("42", "bar.txt");
        AreEqual(new TaskSigner().Digest(fooFile).Digest("9001").Digest(barFile).Digest("42").Finish().Hash,
                 new TaskSigner().DigestSources(new[] {fooFile, barFile}).Finish().Hash);
      }
    }

    [Test]
    [Ignore("TODO: Implement")]
    public void Hash_of_foo_and_bar_should_not_equal_hash_of_foobar() {}
  }
}