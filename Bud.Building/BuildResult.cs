﻿using System.Collections.Immutable;

namespace Bud {
  /// <summary>
  ///   Contains information about outcome of a build. For example: a list of output files and the signature of the
  ///   task.
  ///
  ///   Instances of this class are returned by the <see cref="BuildTask.Execute"/> method.
  /// </summary>
  public class BuildResult {
    /// <summary>
    ///   Files generated by a <see cref="BuildTask"/>.
    /// </summary>
    public ImmutableHashSet<string> OutputFiles { get; }

    /// <summary>
    ///   Initializes a new instance of the <see cref="BuildResult"/> class.
    /// </summary>
    /// <param name="outputFiles">see <see cref="OutputFiles"/>.</param>
    public BuildResult(ImmutableHashSet<string> outputFiles) {
      OutputFiles = outputFiles;
    }
  }
}