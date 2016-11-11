namespace Bud {
  internal class BuildTaskNumberAssigner {
    private int lastAssignedNumber;

    public BuildTaskNumberAssigner(int totalTasks) {
      TotalTasks = totalTasks;
    }

    public int TotalTasks { get; }

    public int AssignNumber() => ++lastAssignedNumber;
  }
}