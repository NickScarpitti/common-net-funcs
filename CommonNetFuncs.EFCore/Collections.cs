using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CommonNetFuncs.EFCore;

public static class Collections
{
	/// <summary>
	/// Select object from an <see cref="IQueryable{T}"/> by matching all non-null fields to an object of the same type comprising the collection.
	/// This overload is for database specific queries using EF Core where DateTime values may need to be compared.
	/// </summary>
	/// <typeparam name="T">Object type.</typeparam>
	/// <param name="queryable">Queryable collection to select from</param>
	/// <param name="context">EF Core DB Context</param>
	/// <param name="partialObject">Object with fields to match with objects in the queryable collection</param>
	/// <param name="ignoreDefaultValues">Optional: Ignore default values in retrieval when true. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: The cancellation token for this operation.</param>
	/// <returns>First object that matches all non-null fields in <paramref name="partialObject"/></returns>
	public static T? GetObjectByPartial<T>(this IQueryable<T> queryable, DbContext context, T partialObject, bool ignoreDefaultValues = true, CancellationToken cancellationToken = default) where T : class
	{
		Type entityType = typeof(T);
		PropertyInfo[] properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		ParameterExpression parameter = Expression.Parameter(entityType, "$x");
		Expression? conditions = null;

		// Get EF Core metadata for the entity
		IEntityType? entityTypeMetadata = context.Model.FindEntityType(entityType);

		foreach (PropertyInfo property in properties)
		{
			cancellationToken.ThrowIfCancellationRequested();

			object? partialValue = property.GetValue(partialObject);

			if (partialValue is null)
			{
				continue;
			}

			if (ignoreDefaultValues)
			{
				object? defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
				if (Equals(partialValue, defaultValue))
				{
					continue;
				}
			}

			// Normalize DateTime values based on EF Core metadata
			if (partialValue is DateTime dateTimeValue)
			{
				partialValue = NormalizeDateTimeForDatabase(dateTimeValue, property, entityTypeMetadata, context);
			}

			BinaryExpression condition = Expression.Equal(Expression.Property(parameter, property), Expression.Constant(partialValue, property.PropertyType));

			conditions = conditions == null ? condition : Expression.AndAlso(conditions, condition);
		}

		T? model = null;
		if (conditions != null)
		{
			// Build the final lambda expression and execute the query
			Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(conditions, parameter);
			model = queryable.FirstOrDefault(lambda);
		}
		return model;

		//if (conditions == null)
		//{
		//	return await context.Set<T>().ToListAsync(cancellationToken);
		//}

		//Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(conditions, parameter);
		//return await context.Set<T>().Where(lambda).ToListAsync(cancellationToken);
	}

	private static DateTime NormalizeDateTimeForDatabase(DateTime dateTimeValue, PropertyInfo property, IEntityType? entityTypeMetadata, DbContext context)
	{
		IProperty? efProperty = entityTypeMetadata?.FindProperty(property.Name);

		if (efProperty == null)
		{
			return dateTimeValue.ToUniversalTime();
		}

		// Check which database provider we're using
		string? providerName = context.Database.ProviderName?.ToLowerInvariant();

		// For non-relational providers (e.g., in-memory), convert Local to UTC for comparison
		// In-memory database stores DateTime values but Local times need UTC conversion
		if (providerName?.Contains("inmemory") == true)
		{
			return dateTimeValue.Kind == DateTimeKind.Local
				? dateTimeValue.ToUniversalTime()
				: dateTimeValue;
		}

		// GetColumnType() only works for relational providers
		string? storeType;
		try
		{
			storeType = efProperty.GetColumnType()?.ToLowerInvariant();
		}
		catch (InvalidCastException)
		{
			// If we can't get column type (non-relational provider), default to UTC
			return dateTimeValue.ToUniversalTime();
		}

		if (string.IsNullOrEmpty(storeType))
		{
			return dateTimeValue.ToUniversalTime();
		}

		if (providerName?.Contains("postgres") == true || providerName?.Contains("npgsql") == true)
		{
			// PostgreSQL: BOTH timestamp and timestamptz need UTC for queries
			return dateTimeValue.ToUniversalTime();
		}
		else if (providerName?.Contains("sqlserver") == true)
		{
			// SQL Server: datetime/datetime2 need Unspecified, datetimeoffset needs UTC
			bool isTimezoneAware = storeType.Contains("datetimeoffset");
			return isTimezoneAware
					? dateTimeValue.ToUniversalTime()
					: DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Unspecified);
		}
		else if (providerName?.Contains("mysql") == true)
		{
			// MySQL: datetime columns work with Unspecified
			return DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Unspecified);
		}
		else
		{
			// Default: UTC is safest for most providers
			return dateTimeValue.ToUniversalTime();
		}
	}

}
