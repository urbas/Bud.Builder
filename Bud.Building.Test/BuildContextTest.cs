using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Moq;
using NUnit.Framework;

namespace Bud {
  public class BuildContextTest {
    [Test]
    public void MarkTaskFinished_throws_exception_when_two_build_tasks_share_same_signature() {
      var ctx = new BuildContext(new StringWriter(), new Stopwatch(), 1, 1, "/foo/bar",
                                 new ConcurrentDictionary<string, BuildTask>(),
                                 new ConcurrentDictionary<string, BuildTask>());

      var buildTaskFoo = new Mock<BuildTask>();
      buildTaskFoo.Setup(b => b.ToString()).Returns("foo");

      var buildTaskBar = new Mock<BuildTask>();
      buildTaskBar.Setup(b => b.ToString()).Returns("bar");

      ctx.MarkTaskFinished(buildTaskFoo.Object, "foo");
      var exception = Assert.Throws<Exception>(() => ctx.MarkTaskFinished(buildTaskBar.Object, "foo"));

      Assert.AreEqual("Clashing build specification. Found duplicate tasks: 'foo' and 'bar'.", exception.Message);
    }
  }
}