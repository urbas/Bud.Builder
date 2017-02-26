namespace Bud {
  /// <summary>
  ///   This function performs the actual work in the glob-to-extension build tasks.
  /// </summary>
  /// <param name="ctx">this object provides this function with information such as
  /// the list of sources, the output directory, the extension of files produced by the build,
  /// the logger, and some helper functions through which to invoke external compilers.</param>
  public delegate void GlobBuildCommand(GlobBuildContext ctx);
}