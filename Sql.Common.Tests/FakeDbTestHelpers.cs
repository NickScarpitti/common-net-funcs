using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Sql.Common.Tests;

/// <summary>Minimal <see cref="DbParameter"/> used to exercise type-inference fallback paths for parameters that are not driver-specific types.</summary>
internal sealed class FakeDbParameter : DbParameter
{
	public override DbType DbType { get; set; }

	public override ParameterDirection Direction { get; set; }

	public override bool IsNullable { get; set; }

	[AllowNull]
	public override string ParameterName { get; set; } = string.Empty;

	[AllowNull]
	public override string SourceColumn { get; set; } = string.Empty;

	public override object? Value { get; set; }

	public override bool SourceColumnNullMapping { get; set; }

	public override int Size { get; set; }

	public override void ResetDbType() { }
}

/// <summary>Minimal <see cref="DbParameterCollection"/> backing <see cref="FakeDbCommand"/>.</summary>
internal sealed class FakeDbParameterCollection : DbParameterCollection
{
	private readonly List<DbParameter> parameters = [];

	public override int Count => parameters.Count;

	public override object SyncRoot { get; } = new();

	public override int Add(object value)
	{
		parameters.Add((DbParameter)value);
		return parameters.Count - 1;
	}

	public override void AddRange(Array values)
	{
		foreach (object? value in values)
		{
			Add(value!);
		}
	}

	public override void Clear() => parameters.Clear();

	public override bool Contains(object value) => parameters.Contains((DbParameter)value);

	public override bool Contains(string value) => parameters.Exists(p => p.ParameterName == value);

	public override void CopyTo(Array array, int index) => ((ICollection)parameters).CopyTo(array, index);

	public override IEnumerator GetEnumerator() => parameters.GetEnumerator();

	protected override DbParameter GetParameter(int index) => parameters[index];

	protected override DbParameter GetParameter(string parameterName) => parameters.First(p => p.ParameterName == parameterName);

	public override int IndexOf(object value) => parameters.IndexOf((DbParameter)value);

	public override int IndexOf(string parameterName) => parameters.FindIndex(p => p.ParameterName == parameterName);

	public override void Insert(int index, object value) => parameters.Insert(index, (DbParameter)value);

	public override void Remove(object value) => parameters.Remove((DbParameter)value);

	public override void RemoveAt(int index) => parameters.RemoveAt(index);

	public override void RemoveAt(string parameterName) => parameters.RemoveAll(p => p.ParameterName == parameterName);

	protected override void SetParameter(int index, DbParameter value) => parameters[index] = value;

	protected override void SetParameter(string parameterName, DbParameter value)
	{
		int index = IndexOf(parameterName);
		parameters[index] = value;
	}
}

/// <summary>Minimal <see cref="DbCommand"/> that allows arbitrary <see cref="DbParameter"/> instances to be attached for testing purposes.</summary>
internal sealed class FakeDbCommand : DbCommand
{
	private readonly FakeDbParameterCollection parameters = new();

	[AllowNull]
	public override string CommandText { get; set; } = string.Empty;

	public override int CommandTimeout { get; set; }

	public override CommandType CommandType { get; set; }

	public override bool DesignTimeVisible { get; set; }

	public override UpdateRowSource UpdatedRowSource { get; set; }

	protected override DbConnection? DbConnection { get; set; }

	protected override DbParameterCollection DbParameterCollection => parameters;

	protected override DbTransaction? DbTransaction { get; set; }

	public override void Cancel() { }

	public override int ExecuteNonQuery() => 0;

	public override object? ExecuteScalar() => null;

	public override void Prepare() { }

	protected override DbParameter CreateDbParameter() => new FakeDbParameter();

	protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
}
