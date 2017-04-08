using System.Collections.Immutable;

namespace Bud {
  /// <summary>
  ///   Contains the following information about a particular <see cref="IBuildTask"/>:
  ///
  ///   <ul>
  ///     <li>output directory directory into which the <see cref="IBuildTask"/> has placed its output,</li>
  ///     <li>the signature of the <see cref="IBuildTask"/>,</li>
  ///     <li>the results of the dependencies of the <see cref="IBuildTask"/>,</li>
  ///   </ul>
  /// </summary>
  public class BuildTaskResult {
    /// <summary>
    ///   The signature of the task tha produced the output.
    /// </summary>
    public string TaskSignature { get; }
    /// <summary>
    ///   This directory contains all the output files produced by the task.
    /// </summary>
    public string TaskOutputDir { get; }
    /// <summary>
    ///   Results of tasks on which this task depends.
    /// </summary>
    public ImmutableArray<BuildTaskResult> DependenciesResults { get; }

    /// <summary>
    ///   Initializes an instance of the build task result.
    /// </summary>
    public BuildTaskResult(string taskSignature, string taskOutputDir,
                           ImmutableArray<BuildTaskResult> dependenciesResults) {
      TaskSignature = taskSignature;
      TaskOutputDir = taskOutputDir;
      DependenciesResults = dependenciesResults;
    }
  }
}