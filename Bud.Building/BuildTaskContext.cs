namespace Bud {
  public class BuildTaskContext {
    public string OutputDir { get; }
    public string SourceDir { get; }

    public BuildTaskContext(string outputDir, string sourceDir) {
      OutputDir = outputDir;
      SourceDir = sourceDir;
    }
  }
}