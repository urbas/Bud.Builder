using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Bud {
  /// <summary>
  ///   Provides a bunch of information to each task.
  /// </summary>
  public class BuildContext : IBuildContext {
    private readonly ConcurrentDictionary<string, BuildTask> signatures2Tasks;

    /// <param name="stdout"><see cref="Stdout"/></param>
    /// <param name="buildStopwatch"><see cref="BuildStopwatch"/></param>
    /// <param name="thisTaskNumber"><see cref="ThisTaskNumber"/></param>
    /// <param name="totalTasks"><see cref="TotalTasks"/></param>
    /// <param name="baseDir"><see cref="BaseDir"/></param>
    public BuildContext(TextWriter stdout, Stopwatch buildStopwatch, int thisTaskNumber, int totalTasks,
                        string baseDir, ConcurrentDictionary<string, BuildTask> signatures2Tasks) {
      Stdout = stdout;
      BuildStopwatch = buildStopwatch;
      ThisTaskNumber = thisTaskNumber;
      TotalTasks = totalTasks;
      BaseDir = baseDir;
      this.signatures2Tasks = signatures2Tasks;
      TaskSignaturesDir = Path.Combine(BaseDir, BuildExecution.TaskSignaturesDirName);
    }

    /// <summary>
    ///   The output to which to write all output of this build task.
    /// </summary>
    public TextWriter Stdout { get; }

    /// <summary>
    ///   this stopwatch will be stopping time since the moment the user invoked the build.
    /// </summary>
    public Stopwatch BuildStopwatch { get; }

    /// <summary>
    ///   The number of the task to which this context belongs. Every build task in the task graph is
    ///   assigned a number with the following properties:
    ///
    ///   <para>
    ///     - every task number number is a positive integer between 1 and the total number of tasks,
    ///   </para>
    ///
    ///   <para>
    ///     - no two tasks share the same number, and
    ///   </para>
    ///
    ///   <para>
    ///     - the task number of a task is larger than any of the task numbers of its dependencies.
    ///   </para>
    /// </summary>
    public int ThisTaskNumber { get; }

    /// <summary>
    ///   the total number of tasks in a build graph that is currently being executed.
    /// </summary>
    public int TotalTasks { get; }

    /// <summary>
    ///   This is the directory where the build was executed.
    /// </summary>
    public string BaseDir { get; }

    /// <summary>
    /// The directory where task signature files will be stored.
    /// </summary>
    /// <remarks>
    /// Task signature files mark the completion of a particular task. An exception will be thrown during the build
    /// if signatures of two tasks are the same.
    /// </remarks>
    public string TaskSignaturesDir { get; }

    public void MarkTaskFinished(BuildTask buildTask, string taskSignature) {
      var storedTask = signatures2Tasks.GetOrAdd(taskSignature, buildTask);
      if (storedTask != buildTask) {
        throw new Exception($"Clashing build specification. Found duplicate tasks: '{storedTask}' and '{buildTask}'.");
      }
    }
  }
}