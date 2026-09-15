#if CORE_NATIVE_BUILD
using System.Data;
using Microsoft.Data.SqlClient;
using SqlServerDebug = CommonNetFuncs.Sql.SqlServer.Debug;

namespace Sql.Common.Tests;

public sealed class SqlServerDebugTests
{
	[Fact]
	public void RenderCommandAsScript_ShouldRenderCommandText_WhenNoParameters()
	{
		using SqlCommand cmd = new("SELECT * FROM TestTable");

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldNotContain("Parameters");
		result.ShouldContain("-- ==== Command ====");
		result.ShouldContain("SELECT * FROM TestTable");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldDeclareVarcharParameter_WithSize()
	{
		using SqlCommand cmd = new("SELECT * FROM TestTable WHERE Name = @Name");
		cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 50) { Value = "O'Brien" });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("DECLARE @Name NVARCHAR(50) = N'O''Brien';");
		result.ShouldContain("SELECT * FROM TestTable WHERE Name = @Name");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldDeclareVarcharParameter_AsMax_WhenSizeIsZeroOrNegative()
	{
		using SqlCommand cmd = new("SELECT @Description");
		cmd.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, -1) { Value = "text" });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("DECLARE @Description NVARCHAR(MAX) = N'text';");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldAddAtSignPrefix_WhenParameterNameLacksIt()
	{
		using SqlCommand cmd = new("SELECT @Id");
		cmd.Parameters.Add(new SqlParameter("Id", SqlDbType.Int) { Value = 5 });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("DECLARE @Id INT = 5;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldDeclareDecimalParameter_WithPrecisionAndScale()
	{
		using SqlCommand cmd = new("SELECT @Amount");
		SqlParameter parameter = new("@Amount", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = 123.45m };
		cmd.Parameters.Add(parameter);

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("DECLARE @Amount DECIMAL(10,2) = 123.45;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldDeclareDecimalParameter_WithDefaultPrecision_WhenPrecisionIsZero()
	{
		// Use a FakeDbParameter so SqlParameter's auto-inferred Precision/Scale from Value doesn't mask the zero-precision default path
		using FakeDbCommand cmd = new() { CommandText = "SELECT @Amount" };
		cmd.Parameters.Add(new FakeDbParameter { ParameterName = "@Amount", DbType = DbType.Decimal, Value = 1.5m });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("DECLARE @Amount DECIMAL(18,0) = 1.5;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatNullValue_AsNull()
	{
		using SqlCommand cmd = new("SELECT @Value");
		cmd.Parameters.Add(new SqlParameter("@Value", SqlDbType.NVarChar, 50) { Value = null });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= NULL;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatDbNullValue_AsNull()
	{
		using SqlCommand cmd = new("SELECT @Value");
		cmd.Parameters.Add(new SqlParameter("@Value", SqlDbType.Int) { Value = DBNull.Value });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= NULL;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatBooleanValue_AsBit()
	{
		using SqlCommand cmd = new("SELECT @Flag");
		cmd.Parameters.Add(new SqlParameter("@Flag", SqlDbType.Bit) { Value = true });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= 1;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatDateTimeValue_WithQuotes()
	{
		using SqlCommand cmd = new("SELECT @CreatedDate");
		DateTime date = new(2024, 5, 17, 13, 45, 30, 123);
		cmd.Parameters.Add(new SqlParameter("@CreatedDate", SqlDbType.DateTime2) { Value = date });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= '2024-05-17 13:45:30.123';");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatDateTimeOffsetValue_WithOffset()
	{
		using SqlCommand cmd = new("SELECT @CreatedDate");
		DateTimeOffset date = new(2024, 5, 17, 13, 45, 30, 123, TimeSpan.FromHours(-5));
		cmd.Parameters.Add(new SqlParameter("@CreatedDate", SqlDbType.DateTimeOffset) { Value = date });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= '2024-05-17 13:45:30.123 -05:00';");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatGuidValue_WithQuotes()
	{
		using SqlCommand cmd = new("SELECT @Id");
		Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
		cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= '11111111-2222-3333-4444-555555555555';");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatByteArrayValue_AsHexLiteral()
	{
		using SqlCommand cmd = new("SELECT @Data");
		byte[] data = [0xDE, 0xAD, 0xBE, 0xEF];
		cmd.Parameters.Add(new SqlParameter("@Data", SqlDbType.VarBinary) { Value = data });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= 0xDEADBEEF;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldFormatNumericValues_UsingInvariantCulture()
	{
		using SqlCommand cmd = new("SELECT @IntVal, @DoubleVal, @LongVal");
		cmd.Parameters.Add(new SqlParameter("@IntVal", SqlDbType.Int) { Value = 42 });
		cmd.Parameters.Add(new SqlParameter("@DoubleVal", SqlDbType.Float) { Value = 3.14 });
		cmd.Parameters.Add(new SqlParameter("@LongVal", SqlDbType.BigInt) { Value = 123456789012345L });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= 42;");
		result.ShouldContain("= 3.14;");
		result.ShouldContain("= 123456789012345;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldInferSqlDbType_WhenParameterIsNotSqlParameter()
	{
		using FakeDbCommand cmd = new() { CommandText = "SELECT @Value" };
		cmd.Parameters.Add(new FakeDbParameter { ParameterName = "@Value", Value = 42 });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("DECLARE @Value INT = 42;");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldEscapeUnmatchedType_AsQuotedNString()
	{
		using FakeDbCommand cmd = new() { CommandText = "SELECT @Value" };
		cmd.Parameters.Add(new FakeDbParameter { ParameterName = "@Value", Value = new Uri("https://example.com") });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		result.ShouldContain("= N'https://example.com/';");
	}

	[Fact]
	public void RenderCommandAsScript_ShouldRenderMultipleParameters_InOrder()
	{
		using SqlCommand cmd = new("SELECT * FROM TestTable WHERE Id = @Id AND Name = @Name");
		cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = 1 });
		cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 50) { Value = "Test" });

		string result = SqlServerDebug.RenderCommandAsScript(cmd);

		int idIndex = result.IndexOf("DECLARE @Id", StringComparison.Ordinal);
		int nameIndex = result.IndexOf("DECLARE @Name", StringComparison.Ordinal);

		idIndex.ShouldBeGreaterThanOrEqualTo(0);
		nameIndex.ShouldBeGreaterThan(idIndex);
		result.ShouldContain("SELECT * FROM TestTable WHERE Id = @Id AND Name = @Name");
	}
}
#endif
