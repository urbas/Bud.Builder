namespace Bud {
  /// <summary>
  ///   This function performs the build action (such as invoking a compiler in an external process).
  /// </summary>
  /// <param name="ctx">
  ///   This object contains information such as sources to build and output target paths.
  ///   In addition, this object provides logging support, process execution helper methods, etc.
  /// </param>
  public delegate void BuildAction(BuildContext ctx);
}