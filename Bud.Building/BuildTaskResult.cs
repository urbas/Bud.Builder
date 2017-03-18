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
    public string TaskSignature { get; }
    public string TaskOutputDir { get; }
    public ImmutableArray<BuildTaskResult> DependenciesResults { get; }

    public BuildTaskResult(string taskSignature, string taskOutputDir,
                           ImmutableArray<BuildTaskResult> dependenciesResults) {
      TaskSignature = taskSignature;
      TaskOutputDir = taskOutputDir;
      DependenciesResults = dependenciesResults;
    }
  }
}