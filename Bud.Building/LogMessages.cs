using System.Diagnostics;

namespace Bud {
  /// <summary>
  ///   This class contains functions for logging build events in a standard format.
  /// </summary>
  public static class LogMessages {
    private const int ReservedTimeStringLength = 7;

    /// <summary>
    ///    Prints a standard log line explaining that a build task has ended.
    /// </summary>
    public static void LogBuildEnd(BuildContext ctx, string taskDescription = null) {
      var message = string.IsNullOrEmpty(taskDescription) ? "Done building." : $"Done building: {taskDescription}.";
      WriteLogLine(ctx, message);
    }

    /// <summary>
    ///    Prints a standard log line explaining that a build task has ended.
    /// </summary>
    public static void LogBuildStart(BuildContext ctx, string taskDescription = null) {
      var message = string.IsNullOrEmpty(taskDescription) ? "Started building." : $"Started building: {taskDescription}.";
      WriteLogLine(ctx, message);
    }

    /// <summary>
    ///   Writes a log line to the output in the given context. The log line will be formatted in this way:
    ///   <c>[123/321 785.323s] Log Message</c>
    /// </summary>
    /// <param name="ctx">
    ///   this object provides the information about the task and also provides the logging sink
    ///   to which to print the log line.
    /// </param>
    /// <param name="msg">the message to appear at the end of the log line.</param>
    public static void WriteLogLine(BuildContext ctx, string msg)
      => ctx.Stdout.WriteLine("[{0}/{1} {2}s] {3}", ctx.ThisTaskNumber, ctx.TotalTasks,
                              GetTimestamp(ctx.BuildStopwatch), msg);

    private static string GetTimestamp(Stopwatch buildStopwatch)
      => ((double) buildStopwatch.ElapsedMilliseconds / 1000).ToString("F3")
                                                             .PadLeft(ReservedTimeStringLength);
  }
}