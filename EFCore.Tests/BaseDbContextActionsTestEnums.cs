namespace EFCore.Tests;

/// <summary>
/// Represents CRUD operation types for test consolidation
/// </summary>
public enum CrudOperation
{
	Create,
	CreateMany,
	Update,
	UpdateMany,
	DeleteByKey,
	DeleteByObject,
	DeleteMany
}

/// <summary>
/// Represents whether to use full query (with navigation properties) or not
/// </summary>
public enum QueryType
{
	Standard,
	Full
}

/// <summary>
/// Represents whether to remove navigation properties
/// </summary>
public enum NavigationPropertyHandling
{
	Keep,
	Remove
}

/// <summary>
/// Represents entity tracking options
/// </summary>
public enum TrackingBehavior
{
	NoTracking,
	WithTracking
}

/// <summary>
/// Represents split query override options
/// </summary>
public enum SplitQueryOption
{
	Null,
	False,
	True
}

/// <summary>
/// Represents global filter options
/// </summary>
public enum GlobalFilterMode
{
	None,
	DisableAll,
	DisableSpecific
}

/// <summary>
/// Represents streaming vs non-streaming query execution
/// </summary>
public enum ExecutionMode
{
	Synchronous,
	Streaming
}

/// <summary>
/// Represents projection vs entity return type
/// </summary>
public enum ProjectionMode
{
	Entity,
	Projection
}
