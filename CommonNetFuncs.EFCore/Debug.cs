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

			string? dbType = match.Groups["dbType"].Success ? match.Groups["dbType"].Value : null;
			string literal = match.Groups["array"].Success ? FormatPostgreSqlArrayLiteral(match.Groups["array"].Value) :
				match.Groups["null"].Success ? "NULL" : FormatPostgreSqlLiteral(match.Groups["value"].Value, dbType);
			parameters.Add((match.Groups["name"].Value, literal));
			commandStartIndex++;
		}

		string commandText = string.Join('\n', lines.Skip(commandStartIndex)).TrimStart('\n');
		foreach ((string name, string literal) in parameters)
		{
			string escapedLiteral = literal.Replace("$", "$$");
			if (literal.StartsWith("ARRAY[", StringComparison.Ordinal))
			{
				// Hand-written raw SQL sometimes already wraps an array parameter in an explicit ARRAY[...] literal (e.g. "ARRAY[@p14]");
				// replace that whole wrapper first so the self-contained array literal below isn't nested inside another ARRAY[...].
				commandText = Regex.Replace(commandText, $@"ARRAY\s*\[\s*@{Regex.Escape(name)}(?!\w)\s*\]", escapedLiteral, RegexOptions.IgnoreCase);
			}

			commandText = Regex.Replace(commandText, $@"@{Regex.Escape(name)}(?!\w)", escapedLiteral);
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

	/// <summary>Formats a Npgsql array-parameter comment value (e.g. "{ '175', '176' }") into a self-contained "ARRAY[...]" literal.</summary>
	private static string FormatPostgreSqlArrayLiteral(string arrayValue)
	{
		string inner = arrayValue.Trim('{', '}').Trim();
		if (inner.Length == 0)
		{
			return "ARRAY[NULL::text]"; // Element type is unknown for an empty array; a single untyped NULL keeps the literal valid.
		}

		MatchCollection elementMatches = ArrayElementRegex().Matches(inner);
		IEnumerable<string> elements = elementMatches.Count > 0 ?
			elementMatches.Select(elementMatch => FormatPostgreSqlArrayElementLiteral(elementMatch.Groups[1].Value)) :
			inner.Split(',', StringSplitOptions.TrimEntries).Select(FormatPostgreSqlArrayElementLiteral);

		return $"ARRAY[{string.Join(", ", elements)}]";
	}

	/// <summary>
	/// Npgsql always renders array-parameter elements as quoted strings in the debug comment regardless of the underlying element
	/// type (e.g. "{ '1', '2' }" for an int[] parameter), so - unlike scalar parameters - elements are always kept as text literals
	/// instead of being type-sniffed; this also keeps casts like "col::text LIKE ANY(ARRAY[...])" type-valid.
	/// </summary>
	private static string FormatPostgreSqlArrayElementLiteral(string rawValue)
	{
		return $"'{rawValue.Replace("'", "''")}'";
	}

	/// <summary>
	/// Matches a single Npgsql debug-view parameter comment line. The name is prefixed with "@" for LINQ-translated named
	/// parameters (e.g. "-- @name='value' (DbType = X)") but not for positional raw-SQL parameters (e.g. "-- p0='value'",
	/// "-- p1={ 'a', 'b' } (DbType = X)", or "-- p2=NULL (DbType = X)").
	/// </summary>
	[GeneratedRegex(@"^-- @?(?<name>\w+)=(?:'(?<value>.*)'|(?<array>\{.*\})|(?<null>NULL))(?: \(Nullable = \w+\))?(?: \(DbType = (?<dbType>\w+)\))?$")]
	private static partial Regex ParameterCommentLineRegex();

	/// <summary>Matches a single quoted element within an array-parameter comment value, e.g. the "'175'" in "{ '175', '176' }".</summary>
	[GeneratedRegex(@"'((?:[^']|'')*)'")]
	private static partial Regex ArrayElementRegex();

	[GeneratedRegex(@"[+-]\d{2}:\d{2}$")]
	private static partial Regex TimeZoneOffsetSuffixRegex();
}
