using CommonNetFuncs.Web.Api.TaskQueuing.EndpointQueue;

namespace CommonNetFuncs.Web.Api.TaskQueuing;

public class PrioritizedQueuedTask(Func<CancellationToken, Task<object?>> taskFunction) : QueuedTask(taskFunction), IComparable<PrioritizedQueuedTask>
{
	public int Priority { get; set; } // Higher number = higher priority

	public TaskPriority PriorityLevel { get; set; }

	public TimeSpan? Timeout { get; set; }

	public CancellationTokenSource CancellationTokenSource { get; set; } = new();

	public bool IsCancelled => CancellationTokenSource.Token.IsCancellationRequested;

	public int CompareTo(PrioritizedQueuedTask? other)
	{
		if (other == null)
		{
			return 1;
		}

		// Higher priority first, then FIFO for same priority
		int priorityComparison = other.Priority.CompareTo(Priority);
		return priorityComparison != 0 ? priorityComparison : QueuedAt.CompareTo(other.QueuedAt);
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (obj is null)
		{
			return false;
		}

		throw new NotImplementedException();
	}

	public override int GetHashCode()
	{
		throw new NotImplementedException();
	}

	public static bool operator ==(PrioritizedQueuedTask? left, PrioritizedQueuedTask? right)
	{
		if (left is null)
		{
			return right is null;
		}

		return left.Equals(right);
	}

	public static bool operator !=(PrioritizedQueuedTask? left, PrioritizedQueuedTask? right)
	{
		return !(left == right);
	}

	public static bool operator <(PrioritizedQueuedTask? left, PrioritizedQueuedTask? right)
	{
		return left is null ? right is not null : left.CompareTo(right) < 0;
	}

	public static bool operator <=(PrioritizedQueuedTask? left, PrioritizedQueuedTask? right)
	{
		return left is null || left.CompareTo(right) <= 0;
	}

	public static bool operator >(PrioritizedQueuedTask? left, PrioritizedQueuedTask? right)
	{
		return left?.CompareTo(right) > 0;
	}

	public static bool operator >=(PrioritizedQueuedTask? left, PrioritizedQueuedTask? right)
	{
		return left is null ? right is null : left.CompareTo(right) >= 0;
	}
}
