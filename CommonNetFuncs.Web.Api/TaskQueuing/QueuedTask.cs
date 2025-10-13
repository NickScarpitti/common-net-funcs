namespace CommonNetFuncs.Web.Api.TaskQueuing;

public class QueuedTask(Func<CancellationToken, Task<object?>> taskFunction)
{
  public string Id { get; set; } = Guid.NewGuid().ToString();

  public Func<CancellationToken, Task<object?>> TaskFunction { get; set; } = taskFunction;

  public TaskCompletionSource<object?> CompletionSource { get; set; } = new();

  public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
}
