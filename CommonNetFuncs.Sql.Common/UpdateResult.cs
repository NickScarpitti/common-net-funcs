namespace CommonNetFuncs.Sql.Common;

public sealed class UpdateResult(int recordsChanged = default, bool success = default)
{
  public int RecordsChanged { get; set; } = recordsChanged;

  public bool Success { get; set; } = success;
}
