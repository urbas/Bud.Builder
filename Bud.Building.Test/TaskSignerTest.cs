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
    public void Hash_of_empty_string() {
      AreEqual(ToBytesFromHexString("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"),
               new TaskSigner().Finish().Hash);
    }

    [Test]
    public void Hash_of_string_foo() {
      AreEqual(ToBytesFromHexString("2c26b46b68ffc68ff99b453c1d30413413422d706483bfa0f98a5e886266e7ae"),
               new TaskSigner().Digest("foo").Finish().Hash);
    }
  }
}