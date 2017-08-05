using System;

namespace Bud {
  /// <summary>
  /// This exception is thrown when two build tasks in a build graph share the same signature.
  /// </summary>
  public class BuildTaskClashException : Exception {
    /// <summary>
    /// The first of the two clashing build tasks.
    /// </summary>
    public IBuildTask BuildTask1 { get; }
    
    /// <summary>
    /// The second of the two clashing build tasks.
    /// </summary>
    public IBuildTask BuildTask2 { get; }
    
    /// <summary>
    /// The signature the two build tasks are sharing.
    /// </summary>
    public string ClashingSignature { get; }

    /// <summary>
    /// Stores the given exception parameters into properties.
    /// </summary>
    public BuildTaskClashException(IBuildTask buildTask1, IBuildTask buildTask2, string clashingSignature) {
      BuildTask1 = buildTask1;
      BuildTask2 = buildTask2;
      ClashingSignature = clashingSignature;
    }

    /// <summary>
    /// Describes in plain english which build tasks are clashing with what signature.
    /// </summary>
    public override string Message => $"Tasks '{BuildTask1.Name}' and '{BuildTask2.Name}' are clashing. " +
                                      $"They have the same signature '{ClashingSignature}'.";
  }
}