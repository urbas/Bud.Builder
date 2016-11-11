using System.Collections.Immutable;
using System.IO;

namespace Bud {
  /// <summary>
  ///    This class can find files with a particular extension in the given directory.
  /// </summary>
  public class FilesByExtInDir {
    /// <summary>
    /// Directory in which to search for files with the given extension.
    /// </summary>
    public string Dir { get; }

    /// <summary>
    /// The extension of files to search for.
    /// </summary>
    public string Ext { get; }

    /// <param name="dir">See <see cref="Dir"/>.</param>
    /// <param name="ext">See <see cref="Ext"/>.</param>
    public FilesByExtInDir(string dir = null, string ext = null) {
      Dir = dir;
      Ext = ext;
    }

    /// <summary>
    ///   Find all files in the directory <see cref="Dir"/> that have the <see cref="Ext"/> extension.
    ///   Note that if <see cref="Dir"/> is a relative path, then it will be resovled against
    ///   <paramref name="baseDir"/>.
    /// </summary>
    /// <param name="baseDir">the directory relative to which the path of <see cref="Dir"/> should be resolved. By
    /// default the current working directory is taken.</param>
    /// <returns>an array of found files.</returns>
    public ImmutableArray<string> Find(string baseDir = null) {
      var dir = AbsDir(baseDir);
      if (Directory.Exists(dir)) {
        return Directory.EnumerateFiles(dir, $"*{Ext}", SearchOption.AllDirectories)
                        .ToImmutableArray();
      }
      return ImmutableArray<string>.Empty;
    }

    /// <summary>
    ///   Returns the absolute path of the directory in which to search for the files with the given extension.
    ///   This parh is resolved against the given base directory.
    /// </summary>
    /// <param name="baseDir">the base directory against which to resolve the absolute path of
    /// <see cref="Dir"/>.</param>
    /// <returns>the absolute path of <see cref="Dir"/>.</returns>
    public string AbsDir(string baseDir) {
      if (string.IsNullOrEmpty(Dir)) {
        return baseDir ?? Directory.GetCurrentDirectory();
      }
      return Path.Combine(baseDir ?? Directory.GetCurrentDirectory(), Dir);
    }
  }
}