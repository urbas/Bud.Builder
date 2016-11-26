using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Bud {
  /// <summary>
  ///    This class can find files with a particular extension in the given directory.
  /// </summary>
  public class FileUtils {
    /// <summary>
    ///   Finds all files in the directory <paramref name="dir"/> that have the extension <paramref name="ext"/>.
    /// </summary>
    /// <returns>an array of found files.</returns>
    public static ImmutableArray<string> FindFiles(string dir, string ext = "") {
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

    /// <summary>
    /// Deletes files in the directory with the given file extension that are not in the list of
    /// <paramref name="allowedFiles"/>.
    /// </summary>
    /// <param name="dir">the directory from which to delete files.</param>
    /// <param name="allowedFiles">the list of only allowed files in the directory. The paths in this collection
    /// should be absolute. This method uses the <see cref="ICollection{T}.Contains"/> method to check whether
    /// a path is allowed.</param>
    /// <param name="fileExtension">the extension the files to delete should match. Other files will not be
    /// touched.</param>
    public static void DeleteExtraneousFiles(string dir, ICollection<string> allowedFiles, string fileExtension = "") {
      foreach (var outputFile in FindFiles(dir, fileExtension)) {
        if (!allowedFiles.Contains(outputFile)) {
          File.Delete(outputFile);
        }
      }
    }
  }
}