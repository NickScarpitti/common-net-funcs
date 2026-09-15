#if CORE_NATIVE_BUILD
using Npgsql;
using NpgsqlTypes;
using PostgreSqlDebug = CommonNetFuncs.Sql.PostgreSql.Debug;

namespace Sql.Common.Tests;

public sealed class PostgreSqlDebugTests
{
	[Fact]
	public void RenderCommandAsScript_ShouldRenderCommandText_WhenNoParameters()
	{
		using NpgsqlCommand cmd = new("SELECT * FROM test_table");

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldNotContain("Parameters");
		result.ShouldContain("-- ==== Command ====");
		result.ShouldContain("SELECT * FROM test_table");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldInlineStringParameter_WithAtSignPlaceholder()
	{
		using NpgsqlCommand cmd = new("SELECT * FROM test_table WHERE name = @name");
		cmd.Parameters.Add(new NpgsqlParameter("name", NpgsqlDbType.Varchar) { Value = "O'Brien" });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("-- @name [varchar(unbounded)] = 'O''Brien'");
		result.ShouldContain("SELECT * FROM test_table WHERE name = 'O''Brien'");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldInlineStringParameter_WithColonPlaceholder()
	{
		using NpgsqlCommand cmd = new("SELECT * FROM test_table WHERE name = :name");
		cmd.Parameters.Add(new NpgsqlParameter("name", NpgsqlDbType.Varchar) { Value = "Test" });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("SELECT * FROM test_table WHERE name = 'Test'");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldNotSubstitutePartialNameMatches()
	{
		using NpgsqlCommand cmd = new("SELECT * FROM test_table WHERE id = @id AND id2 = @id2");
		cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Integer) { Value = 1 });
		cmd.Parameters.Add(new NpgsqlParameter("id2", NpgsqlDbType.Integer) { Value = 2 });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("SELECT * FROM test_table WHERE id = 1 AND id2 = 2");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldInlinePositionalParameter_WhenUnnamed()
	{
		using NpgsqlCommand cmd = new("SELECT * FROM test_table WHERE id = $1");
		cmd.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, Value = 7 });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("-- $1 [integer] = 7");
		result.ShouldContain("SELECT * FROM test_table WHERE id = 7");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatNullValue_AsNull()
	{
		using NpgsqlCommand cmd = new("SELECT @value");
		cmd.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Varchar) { Value = null });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= NULL");
		result.ShouldContain("SELECT NULL");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatDbNullValue_AsNull()
	{
		using NpgsqlCommand cmd = new("SELECT @value");
		cmd.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Integer) { Value = DBNull.Value });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= NULL");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatBooleanValue_AsTrueOrFalse()
	{
		using NpgsqlCommand cmd = new("SELECT @flagTrue, @flagFalse");
		cmd.Parameters.Add(new NpgsqlParameter("flagTrue", NpgsqlDbType.Boolean) { Value = true });
		cmd.Parameters.Add(new NpgsqlParameter("flagFalse", NpgsqlDbType.Boolean) { Value = false });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("SELECT TRUE, FALSE");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatDateTimeValue_WithTimestampCast()
	{
		using NpgsqlCommand cmd = new("SELECT @createdDate");
		DateTime date = new(2024, 5, 17, 13, 45, 30, 123);
		cmd.Parameters.Add(new NpgsqlParameter("createdDate", NpgsqlDbType.Timestamp) { Value = date });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("SELECT '2024-05-17 13:45:30.123000'::timestamp");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatDateTimeOffsetValue_WithTimestampTzCast()
	{
		using NpgsqlCommand cmd = new("SELECT @createdDate");
		DateTimeOffset date = new(2024, 5, 17, 13, 45, 30, 123, TimeSpan.FromHours(-5));
		cmd.Parameters.Add(new NpgsqlParameter("createdDate", NpgsqlDbType.TimestampTz) { Value = date });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("SELECT '2024-05-17 13:45:30.123000-05:00'::timestamptz");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatGuidValue_WithUuidCast()
	{
		using NpgsqlCommand cmd = new("SELECT @id");
		Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
		cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("SELECT '11111111-2222-3333-4444-555555555555'::uuid");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatByteArrayValue_AsHexBytea()
	{
		using NpgsqlCommand cmd = new("SELECT @data");
		byte[] data = [0xDE, 0xAD, 0xBE, 0xEF];
		cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlDbType.Bytea) { Value = data });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain(@"SELECT '\xdeadbeef'::bytea");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatNumericValues_UsingInvariantCulture()
	{
		using NpgsqlCommand cmd = new("SELECT @intVal, @doubleVal, @longVal, @decimalVal");
		cmd.Parameters.Add(new NpgsqlParameter("intVal", NpgsqlDbType.Integer) { Value = 42 });
		cmd.Parameters.Add(new NpgsqlParameter("doubleVal", NpgsqlDbType.Double) { Value = 3.14 });
		cmd.Parameters.Add(new NpgsqlParameter("longVal", NpgsqlDbType.Bigint) { Value = 123456789012345L });
		cmd.Parameters.Add(new NpgsqlParameter("decimalVal", NpgsqlDbType.Numeric) { Value = 9.5m });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("SELECT 42, 3.14, 123456789012345, 9.5");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldDeclareNumericType_WithPrecisionAndScale()
	{
		using NpgsqlCommand cmd = new("SELECT @amount");
		cmd.Parameters.Add(new NpgsqlParameter("amount", NpgsqlDbType.Numeric) { Precision = 10, Scale = 2, Value = 123.45m });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("-- @amount [numeric(10,2)] = 123.45");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldDeclareNumericType_WithDefaultPrecision_WhenPrecisionIsZero()
	{
		using NpgsqlCommand cmd = new("SELECT @amount");
		cmd.Parameters.Add(new NpgsqlParameter("amount", NpgsqlDbType.Numeric) { Value = 1.5m });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("-- @amount [numeric(18,0)] = 1.5");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldInferNpgsqlDbType_WhenParameterIsNotNpgsqlParameter()
	{
		using FakeDbCommand cmd = new() { CommandText = "SELECT @value" };
		cmd.Parameters.Add(new FakeDbParameter { ParameterName = "@value", Value = 42 });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("-- @value [integer] = 42");
		result.ShouldContain("SELECT 42");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldEscapeUnmatchedType_AsQuotedString()
	{
		using FakeDbCommand cmd = new() { CommandText = "SELECT @value" };
		cmd.Parameters.Add(new FakeDbParameter { ParameterName = "@value", Value = new Uri("https://example.com") });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("'https://example.com/'");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldRenderMultipleParameters_InOrder()
	{
		using NpgsqlCommand cmd = new("SELECT * FROM test_table WHERE id = @id AND name = @name");
		cmd.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Integer) { Value = 1 });
		cmd.Parameters.Add(new NpgsqlParameter("name", NpgsqlDbType.Varchar) { Value = "Test" });

		string result = PostgreSqlDebug.RenderCommandAsScript(cmd);

		int idIndex = result.IndexOf("-- @id", StringComparison.Ordinal);
		int nameIndex = result.IndexOf("-- @name", StringComparison.Ordinal);

		idIndex.ShouldBeGreaterThanOrEqualTo(0);
		nameIndex.ShouldBeGreaterThan(idIndex);
		result.ShouldContain("SELECT * FROM test_table WHERE id = 1 AND name = 'Test'");
	}
}
#endif
