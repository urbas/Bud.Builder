using System.Collections.Immutable;
using System.Linq;
using Moq;
using NUnit.Framework;
using static Bud.BuildExecution;

namespace Bud {
  public class BuildExecutionTest {
    [Test]
    public void CollectOutputFiles_returns_empty_list_when_given_no_build_tasks()
      => Assert.IsEmpty(CollectOutputFiles(Enumerable.Empty<BuildTask>()));

    [Test]
    public void CollectOutputFiles_returns_files_of_a_signle_build_task() {
      var buildTask = new Mock<BuildTask>();
      var expectedOutputFiles = ImmutableArray.Create("foo");
      buildTask.Setup(bt => bt.OutputFiles).Returns(expectedOutputFiles);

      Assert.AreEqual(expectedOutputFiles, CollectOutputFiles(new []{buildTask.Object}));
    }
  }
}