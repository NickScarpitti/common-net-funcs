namespace CommonNetFuncs.Sql.Common;

public sealed class UpdateResult(int recordsChanged, bool success)
{
  public int RecordsChanged { get; init; } = recordsChanged;

  public bool Success { get; init; } = success;
}
