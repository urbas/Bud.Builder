using System;
using System.Text;
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
      => AreEqual(ToBytesFromHexString("3d762bea849db8b2a3b5c72e974dfad4108aee487c35f150847b49c656ff1ec8"),
                  new TaskSigner().Digest("foo").Finish().Hash);

    [Test]
    public void Hash_of_string_foo_with_small_buffer()
      => AreEqual(ToBytesFromHexString("4c73219fb915977361405854a995bb01c9d2e02a68a5506f5dd3dee924143ec1"),
                  new TaskSigner(new byte[7]).Digest("foo bar ™ glar har").Finish().Hash);

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
        AreEqual(new TaskSigner().Digest(fooFile).Digest(Encoding.UTF8.GetBytes("9001"))
                                 .Digest(barFile).Digest(Encoding.UTF8.GetBytes("42")).Finish().Hash,
                 new TaskSigner().DigestSources(new[] {fooFile, barFile}).Finish().Hash);
      }
    }

  }
}