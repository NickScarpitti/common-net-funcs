using CommonNetFuncs.EFCore;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Tests;

public sealed class DebugTests
{
	private const string SqlServerConnectionString = "Server=localhost;Database=test;Trusted_Connection=true;TrustServerCertificate=true";

	private const string PostgreSqlConnectionString = "Host=localhost;Database=test";

	[Fact]
	public void RenderQueryAsScript_WithSqlServerContext_ReturnsRunnableDeclareScript()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseSqlServer(SqlServerConnectionString));
		string name = "Pink Floyd";

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.Name == name);

		string result = query.RenderQueryAsScript(context);

		result.ShouldContain("DECLARE @name nvarchar(4000) = N'Pink Floyd';");
		result.ShouldContain("@name");
		result.ShouldNotContain("-- ====");
	}

	[Fact]
	public void RenderQueryAsScript_WithSqlDialectSqlServer_ReturnsRunnableDeclareScript()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseSqlServer(SqlServerConnectionString));
		int value = 5;

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.Value == value);

		string result = query.RenderQueryAsScript(SqlDialect.SqlServer);

		result.ShouldContain("DECLARE @value int = 5;");
	}

	[Fact]
	public void RenderQueryAsScript_WithPostgreSqlContext_InlinesStringParameterAsLiteral()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));
		string name = "Pink Floyd";

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.Name == name);

		string result = query.RenderQueryAsScript(context);

		result.ShouldContain("-- ==== Parameters (inlined as literals in the command below) ====");
		result.ShouldContain("-- @name = 'Pink Floyd'");
		result.ShouldContain("-- ==== Command ====");
		string commandText = result[(result.IndexOf("-- ==== Command ====", StringComparison.Ordinal))..];
		commandText.ShouldContain("'Pink Floyd'");
		commandText.ShouldNotContain("@name");
		commandText.ShouldNotContain("DECLARE");
	}

	[Fact]
	public void RenderQueryAsScript_WithPostgreSqlDialect_EscapesEmbeddedQuotes()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));
		string name = "O'Brien";

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.Name == name);

		string result = query.RenderQueryAsScript(SqlDialect.PostgreSql);

		result.ShouldContain("-- @name = 'O''Brien'");
		result.ShouldContain("'O''Brien'");
	}

	[Fact]
	public void RenderQueryAsScript_WithPostgreSqlDialect_FormatsBooleanAsTrueOrFalse()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));
		bool active = true;

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.IsActive == active);

		string result = query.RenderQueryAsScript(SqlDialect.PostgreSql);

		result.ShouldContain("-- @active = TRUE");
	}

	[Fact]
	public void RenderQueryAsScript_WithPostgreSqlDialect_FormatsIntegerAsUnquotedNumber()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));
		int value = 42;

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.Value == value);

		string result = query.RenderQueryAsScript(SqlDialect.PostgreSql);

		result.ShouldContain("-- @value = 42");
	}

	[Fact]
	public void RenderQueryAsScript_WithPostgreSqlDialect_FormatsDecimalAsUnquotedNumber()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));
		decimal amount = 123.45m;

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.Amount == amount);

		string result = query.RenderQueryAsScript(SqlDialect.PostgreSql);

		result.ShouldContain("-- @amount = 123.45");
	}

	[Fact]
	public void RenderQueryAsScript_WithPostgreSqlDialect_FormatsDateTimeWithTimestampCast()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));
		DateTime date = new(2024, 5, 17, 13, 45, 30);

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.CreatedDate == date);

		string result = query.RenderQueryAsScript(SqlDialect.PostgreSql);

		result.ShouldContain("'2024-05-17 13:45:30.000000'::timestamp");
	}

	[Fact]
	public void RenderQueryAsScript_WithPostgreSqlDialect_FormatsGuidAsQuotedString()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));
		Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.UniqueId == id);

		string result = query.RenderQueryAsScript(SqlDialect.PostgreSql);

		result.ShouldContain("-- @id = '11111111-2222-3333-4444-555555555555'");
	}

	[Fact]
	public void RenderQueryAsScript_WithPostgreSqlDialect_RendersMultipleParameters()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));
		string name = "Pink Floyd";
		int value = 5;

		IQueryable<DebugTestEntity> query = context.Entities.Where(e => e.Name == name && e.Value == value);

		string result = query.RenderQueryAsScript(SqlDialect.PostgreSql);

		result.ShouldContain("-- @name = 'Pink Floyd'");
		result.ShouldContain("-- @value = 5");
		result.ShouldContain("'Pink Floyd'");
		result.ShouldContain("= 5");
	}

	[Fact]
	public void RenderQueryAsScript_WithNoParameters_OmitsParametersSection()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));

		IQueryable<DebugTestEntity> query = context.Entities;

		string result = query.RenderQueryAsScript(SqlDialect.PostgreSql);

		result.ShouldNotContain("Parameters");
		result.ShouldContain("-- ==== Command ====");
	}

	[Fact]
	public void RenderQueryAsScript_DeterminesDialectFromProviderName_SqlServer()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseSqlServer(SqlServerConnectionString));

		string result = context.Entities.RenderQueryAsScript(context);

		result.ShouldNotContain("-- ====");
	}

	[Fact]
	public void RenderQueryAsScript_DeterminesDialectFromProviderName_PostgreSql()
	{
		using DebugTestDbContext context = CreateContext(o => o.UseNpgsql(PostgreSqlConnectionString));

		string result = context.Entities.RenderQueryAsScript(context);

		result.ShouldContain("-- ==== Command ====");
	}

	private static DebugTestDbContext CreateContext(Action<DbContextOptionsBuilder<DebugTestDbContext>> configure)
	{
		DbContextOptionsBuilder<DebugTestDbContext> builder = new();
		configure(builder);
		return new DebugTestDbContext(builder.Options);
	}
}

internal sealed class DebugTestEntity
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public int Value { get; set; }

	public bool IsActive { get; set; }

	public decimal Amount { get; set; }

	public DateTime CreatedDate { get; set; }

	public Guid UniqueId { get; set; }
}

internal sealed class DebugTestDbContext(DbContextOptions<DebugTestDbContext> options) : DbContext(options)
{
	public DbSet<DebugTestEntity> Entities => Set<DebugTestEntity>();
}
