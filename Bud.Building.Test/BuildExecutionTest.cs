using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using static Bud.BuildExecution;

namespace Bud {
  public class BuildExecutionTest {
    [Test]
    public void CollectOutputFiles_returns_empty_list_when_given_no_build_tasks()
      => Assert.IsEmpty(CollectOutputFiles(Enumerable.Empty<BuildTask>(), Directory.GetCurrentDirectory()));

    [Test]
    public void CollectOutputFiles_returns_files_of_a_signle_build_task() {

      var buildTask = new Mock<BuildTask>();
      buildTask.Setup(bt => bt.OutputFiles).Returns(ImmutableArray.Create("foo"));

      Assert.AreEqual(ImmutableArray.Create(Path.Combine(Directory.GetCurrentDirectory(), "foo")),
                      CollectOutputFiles(new []{buildTask.Object}, Directory.GetCurrentDirectory()));
    }

    [Test]
    public void CollectOutputFiles_deduplicates_same_files() {

      var buildTask1 = new Mock<BuildTask>();
      buildTask1.Setup(bt => bt.OutputFiles).Returns(ImmutableArray.Create("foo"));
      var buildTask2 = new Mock<BuildTask>();
      buildTask2.Setup(bt => bt.OutputFiles).Returns(ImmutableArray.Create("boo/../foo"));

      Assert.AreEqual(ImmutableArray.Create(Path.Combine(Directory.GetCurrentDirectory(), "foo")),
                      CollectOutputFiles(new []{buildTask1.Object, buildTask2.Object},
                                         Directory.GetCurrentDirectory()));
    }
  }
}