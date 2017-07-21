using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Bud {
  /// <summary>
  ///    This class can find files with a particular extension in the given directory.
  /// </summary>
  public class FileUtils {
    /// <summary>
    ///   Finds all files in the directory <paramref name="dir"/> that have the extension <paramref name="ext"/>.
    /// </summary>
    /// <returns>an array of found files.</returns>
    public static ImmutableArray<string> FindFilesByExt(string dir,
                                                        string ext = "",
                                                        SearchOption searchOption = SearchOption.AllDirectories)
      => FindFiles(dir, $"*{ext}", searchOption);

    /// <summary>
    ///   Enumerates all files in the directory. The returned list will contain relative paths.
    /// </summary>
    /// <param name="dir">the directory in which to look for files.</param>
    /// <param name="searchPattern">the glob pattern of files to find.</param>
    /// <param name="searchOption">indicates whether to search the directory recursively or not.</param>
    /// <returns>a list of relative file paths.</returns>
    public static IEnumerable<string> FindFilesRelative(string dir,
                                                        string searchPattern = "*",
                                                        SearchOption searchOption = SearchOption.AllDirectories) {
      var dirUri = new Uri($"{dir}/");
      return FindFiles(dir, searchPattern, searchOption)
        .Select(path => dirUri.MakeRelativeUri(new Uri(path)).ToString());
    }

    /// <summary>
    ///   Finds all files in the directory. The distinguishing feature of this method when compared to
    ///   <see cref="Directory.EnumerateFiles(string,string,System.IO.SearchOption)"/> is that this method returns
    ///   an empty array if the directory doesn't exist (instead of throwing an exception..
    /// </summary>
    /// <param name="dir">the directory in which to look for files.</param>
    /// <param name="searchPattern">the glob pattern of files to find.</param>
    /// <param name="searchOption">indicates whether to search the directory recursively or not.</param>
    /// <returns>an array of found files.</returns>
    public static ImmutableArray<string> FindFiles(string dir,
                                                   string searchPattern = "*",
                                                   SearchOption searchOption = SearchOption.AllDirectories) {
      if (Directory.Exists(dir)) {
        return Directory.EnumerateFiles(dir, searchPattern, SearchOption.AllDirectories)
                        .ToImmutableArray();
      }
      return ImmutableArray<string>.Empty;
    }
  }
}