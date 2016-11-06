using System.Diagnostics;
using System.IO;

namespace Bud {
  /// <summary>
  ///   This class contains functions for logging build events in a standard format.
  /// </summary>
  public static class LogMessages {
    private const int ReservedTimeStringLength = 7;

    /// <summary>
    ///    Prints a standard log line explaining that a build task has ended.
    /// </summary>
    public static void LogBuildStart(TextWriter logWriter, Stopwatch buildStopwatch, int thisTaskNumber, int totalTasks,
                                     string taskIdString) {
      var message = string.IsNullOrEmpty(taskIdString) ? "Started building." : $"Started building: {taskIdString}.";
      WriteLogLine(logWriter, thisTaskNumber, totalTasks, buildStopwatch, message);
    }

    /// <summary>
    ///    Prints a standard log line explaining that a build task has ended.
    /// </summary>
    public static void LogBuildEnd(TextWriter logWriter, Stopwatch buildStopwatch, int thisTaskNumber, int totalTasks,
                                   string taskIdString) {
      var message = string.IsNullOrEmpty(taskIdString) ? "Done building." : $"Done building: {taskIdString}.";
      WriteLogLine(logWriter, thisTaskNumber, totalTasks, buildStopwatch, message);
    }

    /// <summary>
    ///   Writes a log line to the <paramref name="logWriter"/>. The log line will be formatted in this way:
    ///   <c>[123/321 785.323s] Log Message</c>
    /// </summary>
    /// <param name="logWriter">the sink to which to output the log line.</param>
    /// <param name="taskNumber">the number of the task for which to log the message.</param>
    /// <param name="totalTasks">the total number of tasks in the current build.</param>
    /// <param name="buildStopwatch">
    ///   this stopwatch provides the time since the start of the build. This time is placed in the head of the
    ///   log line.
    /// </param>
    /// <param name="msg">the message to appear at the end of the log line.</param>
    public static void WriteLogLine(TextWriter logWriter, int taskNumber, int totalTasks, Stopwatch buildStopwatch,
                                    string msg)
      => logWriter.WriteLine("[{0}/{1} {2}s] {3}", taskNumber, totalTasks, GetTimestamp(buildStopwatch), msg);

    private static string GetTimestamp(Stopwatch buildStopwatch)
      => ((double) buildStopwatch.ElapsedMilliseconds / 1000).ToString("F3")
                                                             .PadLeft(ReservedTimeStringLength);
  }
}