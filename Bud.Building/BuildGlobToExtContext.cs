using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bud {
  /// <summary>
  /// This class provides build functions with information such as the list of sources that
  /// are supposed to be built, the output directory into which to place output files, the
  /// extension of files produced by the build, the logger, and some helper functions through which
  /// to invoke external compilers.
  /// </summary>
  public class BuildGlobToExtContext {
    public void Command(string executablePath,
                        string args = null,
                        string cwd = null,
                        IDictionary<string, string> env = null) {
    }

    public string OutputDir { get; }
    public ImmutableArray<string> Sources { get; }
  }
}