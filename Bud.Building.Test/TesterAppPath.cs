using System.IO;
using Bud.BuildingTesterApp;

namespace Bud {
  public static class TesterAppPath {
    public static string TesterApp
      => Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location),
                      $"{typeof(Program).Assembly.GetName().Name}.exe");
  }
}