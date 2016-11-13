using System.Collections.Immutable;
using System.IO;

namespace Bud {
  /// <summary>
  ///    This class can find files with a particular extension in the given directory.
  /// </summary>
  public class FilesUtils {
    /// <summary>
    ///   Finds all files in the directory <see cref="dir"/> that have the extension <see cref="ext"/>.
    /// </summary>
    /// <returns>an array of found files.</returns>
    public static ImmutableArray<string> Find(string dir, string ext) {
      if (Directory.Exists(dir)) {
        return Directory.EnumerateFiles(dir, $"*{ext}", SearchOption.AllDirectories)
                        .ToImmutableArray();
      }
      return ImmutableArray<string>.Empty;
    }

    /// <param name="dir">if this directory is a relative path, then it will be resolved against
    /// <paramref name="baseDir"/></param>
    /// <param name="baseDir">the base directory against which to resolve the absolute path of
    /// <paramref name="dir"/>. If this parameter is <c>null</c> then the current working directory will be
    /// taken.</param>
    /// <returns>the absolute path of <paramref name="dir"/> relative to <paramref name="baseDir"/>.</returns>
    public static string ToAbsDir(string dir, string baseDir = null) {
      if (string.IsNullOrEmpty(dir)) {
        return baseDir ?? Directory.GetCurrentDirectory();
      }
      return Path.Combine(baseDir ?? Directory.GetCurrentDirectory(), dir);
    }
  }
}