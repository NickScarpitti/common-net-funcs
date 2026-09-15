using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace CommonNetFuncs.EFCore;

/// <summary>SQL dialect to target when rendering an EF Core query as a runnable script.</summary>
public enum SqlDialect
{
	SqlServer,
	PostgreSql
}

public static partial class Debug
{
	/// <summary>
	/// Renders an EF Core LINQ query as a self-contained SQL script that can be pasted directly into a query tool and executed, with
	/// parameter values inlined. The dialect is auto-detected from <paramref name="context"/>'s configured provider.
	/// FOR DIAGNOSTICS ONLY - the produced text is not safe to execute as user input.
	/// </summary>
	public static string RenderQueryAsScript(this IQueryable query, DbContext context)
	{
		return query.RenderQueryAsScript(GetDialect(context.Database.ProviderName));
	}

	/// <summary>
	/// Renders an EF Core LINQ query as a self-contained SQL script for the specified <paramref name="dialect"/>, with parameter
	/// values inlined. FOR DIAGNOSTICS ONLY - the produced text is not safe to execute as user input.
	/// </summary>
	public static string RenderQueryAsScript(this IQueryable query, SqlDialect dialect)
	{
		string debugViewSql = query.ToQueryString();

		// SQL Server's debug view is already a runnable script (parameters are declared via T-SQL DECLARE statements).
		// PostgreSQL has no top-level DECLARE syntax, so Npgsql's debug view only comments the parameter values out;
		// those need to be inlined as literals into the command text to produce something actually runnable.
		return dialect == SqlDialect.PostgreSql ? RenderPostgreSqlScript(debugViewSql) : debugViewSql;
	}

	private static SqlDialect GetDialect(string? providerName)
	{
		return !string.IsNullOrEmpty(providerName) && providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ? SqlDialect.PostgreSql : SqlDialect.SqlServer;
	}

	private static string RenderPostgreSqlScript(string debugViewSql)
	{
		string[] lines = debugViewSql.Replace("\r\n", "\n").Split('\n');
		List<(string Name, string Literal)> parameters = [];

		int commandStartIndex = 0;
		foreach (string line in lines)
		{
			Match match = ParameterCommentLineRegex().Match(line);
			if (!match.Success)
			{
				break; // Npgsql's parameter comments always precede the command text as a contiguous block
			}

			string literal = FormatPostgreSqlLiteral(match.Groups["value"].Value, match.Groups["dbType"].Success ? match.Groups["dbType"].Value : null);
			parameters.Add((match.Groups["name"].Value, literal));
			commandStartIndex++;
		}

		string commandText = string.Join('\n', lines.Skip(commandStartIndex)).TrimStart('\n');
		foreach ((string name, string literal) in parameters)
		{
			commandText = Regex.Replace(commandText, $@"@{Regex.Escape(name)}(?!\w)", literal.Replace("$", "$$"));
		}

		StringBuilder script = new();
		if (parameters.Count > 0)
		{
			script.AppendLine("-- ==== Parameters (inlined as literals in the command below) ====");
			foreach ((string name, string literal) in parameters)
			{
				script.AppendLine($"-- @{name} = {literal}");
			}
			script.AppendLine();
		}
		script.AppendLine("-- ==== Command ====");
		script.Append(commandText);

		return script.ToString();
	}

	private static string FormatPostgreSqlLiteral(string rawValue, string? dbType)
	{
		if (string.Equals(dbType, "DateTime", StringComparison.Ordinal))
		{
			if (TimeZoneOffsetSuffixRegex().IsMatch(rawValue) && DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dateTimeOffset))
			{
				return $"'{dateTimeOffset:yyyy-MM-dd HH:mm:ss.ffffffzzz}'::timestamptz";
			}

			if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dateTime))
			{
				return $"'{dateTime:yyyy-MM-dd HH:mm:ss.ffffff}'::timestamp";
			}
		}

		return rawValue switch
		{
			"True" => "TRUE",
			"False" => "FALSE",
			_ when decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out _) => rawValue,
			_ => $"'{rawValue.Replace("'", "''")}'"
		};
	}

	/// <summary>Matches a single Npgsql debug-view parameter comment line, e.g. "-- @name='value' (DbType = X)".</summary>
	[GeneratedRegex(@"^-- @(?<name>\w+)='(?<value>.*)'(?: \(DbType = (?<dbType>\w+)\))?$")]
	private static partial Regex ParameterCommentLineRegex();

	[GeneratedRegex(@"[+-]\d{2}:\d{2}$")]
	private static partial Regex TimeZoneOffsetSuffixRegex();
}
