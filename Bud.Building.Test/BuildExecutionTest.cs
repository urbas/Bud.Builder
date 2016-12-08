using System.Linq;
using NUnit.Framework;

namespace Bud {
  public class BuildExecutionTest {
    [Test]
    public void CollectOutputFiles_produces_empty_list_when_given_no_build_tasks()
      => Assert.IsEmpty(BuildExecution.CollectOutputFiles(Enumerable.Empty<BuildTask>()));
  }
}