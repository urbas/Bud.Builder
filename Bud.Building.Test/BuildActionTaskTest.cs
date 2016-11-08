using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using static Bud.Building;

namespace Bud {
  public class BuildActionTaskTest {
    [Test]
    public void RunBuild_executes_the_task() {
      var invoked = false;
      RunBuild(Build(ctx => invoked = true), stdout: new StringWriter());

      Assert.IsTrue(invoked, "The build task was not executed.");
    }

    [Test]
    public void RunBuild_build_start_logged() {
      var buildOutputWriter = new StringWriter();

      RunBuild(Build(context => {}), stdout: buildOutputWriter);

      Assert.That(buildOutputWriter.ToString(),
                  Does.Match(@".*\[1/1\s+\d*(\.\d{3})?s\] Started building\..*"));
    }

    [Test]
    public void RunBuild_build_done_logged() {
      var buildOutputWriter = new StringWriter();

      RunBuild(Build(action: context => {}), stdout: buildOutputWriter);

      Assert.That(buildOutputWriter.ToString(),
                  Does.Match(@".*\[1/1\s+\d*(\.\d{3})?s\] Done building\..*"));
    }

    [Test]
    public void RunBuild_task_number_logged() {
      var buildOutputWriter = new StringWriter();

      RunBuild(Build(action: context => {}, dependsOn: new[] {Build(action: context => {})}),
               stdout: buildOutputWriter);

      Assert.That(buildOutputWriter.ToString(), Does.Contain("[1/2").And.Contains("[2/2"));
    }

    [Test]
    public void RunBuild_task_times_logged() {
      var buildOutputWriter = new StringWriter();

      RunBuild(Build(action: ctx => Thread.Sleep(TimeSpan.FromMilliseconds(10))), stdout: buildOutputWriter);

      var times = Regex.Matches(buildOutputWriter.ToString(), @"\[\d+/\d+\s+(?<time>.+)s\]")
                       .Cast<Match>()
                       .Select(match => double.Parse(match.Groups["time"].Value))
                       .ToList();

      Assert.That(times[1] - times[0], Is.GreaterThan(0.009));
    }

    [Test]
    public void RunBuild_executes_logs_task_name() {
      var buildOutputWriter = new StringWriter();

      RunBuild(Build(context => {}, name: "foo"), stdout: buildOutputWriter);

      Assert.That(buildOutputWriter.ToString(),
                  Does.Contain(@"Started building: foo.").And.Contains(@"Done building: foo."));
    }
  }
}