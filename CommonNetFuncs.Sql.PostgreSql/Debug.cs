using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace CommonNetFuncs.Sql.PostgreSql;

#if NET5_0_OR_GREATER
public static partial class Debug
{
	/// <summary>
	/// Renders a <see cref="DbCommand"/> as a self-contained SQL script that can be pasted directly into
	/// psql / pgAdmin / DBeaver and executed. Unlike T-SQL, PostgreSQL has no top-level DECLARE syntax for
	/// arbitrary statements, so parameter values are inlined as literals directly into the command text
	/// (supporting the "@name", ":name", and positional "$N" placeholder conventions).
	/// FOR DIAGNOSTICS ONLY - the produced text is not safe to execute as user input.
	/// </summary>
	public static string RenderCommandAsScript(DbCommand cmd)
	{
		string commandText = cmd.CommandText;
		StringBuilder script = new();

		if (cmd.Parameters.Count > 0)
		{
			script.AppendLine("-- ==== Parameters (inlined as literals in the command below) ====");
			Dictionary<string, string> literalsByName = new(StringComparer.Ordinal);
			Dictionary<int, string> literalsByOrdinal = [];
			int ordinal = 0;
			foreach (DbParameter p in cmd.Parameters)
			{
				string name = p.ParameterName?.TrimStart('@', ':') ?? string.Empty;
				string literal = FormatSqlLiteral(p.Value);
				string label = !string.IsNullOrEmpty(name) ? $"@{name}" : $"${ordinal + 1}";
				script.AppendLine($"-- {label} [{GetPostgreSqlTypeName(p)}] = {literal}");

				if (!string.IsNullOrEmpty(name))
				{
					literalsByName[name] = literal;
				}
				literalsByOrdinal[ordinal + 1] = literal;
				ordinal++;
			}
			script.AppendLine();

			commandText = SubstituteParameters(commandText, literalsByName, literalsByOrdinal);
		}

		script.AppendLine("-- ==== Command ====");
		script.AppendLine(commandText);

		return script.ToString();
	}

	/// <summary>
	/// Replaces every placeholder in a single pass, so text inside an already-inlined value (e.g. a string containing "@name2") is never re-substituted.
	/// An explicit "ARRAY[@param]" wrapper around an array parameter is replaced as a whole so the array literal isn't nested inside another ARRAY[...].
	/// </summary>
	private static string SubstituteParameters(string commandText, Dictionary<string, string> literalsByName, Dictionary<int, string> literalsByOrdinal)
	{
		return PlaceholderRegex().Replace(commandText, match =>
		{
			bool isWrapped = match.Groups["wrapped"].Success;
			string? literal = null;
			if (match.Groups["name"].Success)
			{
				literalsByName.TryGetValue(match.Groups["name"].Value, out literal);
			}
			else if (int.TryParse(match.Groups["ordinal"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int position))
			{
				literalsByOrdinal.TryGetValue(position, out literal);
			}

			if (literal == null)
			{
				return match.Value;
			}

			return isWrapped && !literal.StartsWith("ARRAY[", StringComparison.Ordinal) && !literal.StartsWith("'{", StringComparison.Ordinal) ?
				$"ARRAY[{literal}]" : literal;
		});
	}

	[GeneratedRegex(@"(?<wrapped>ARRAY\s*\[\s*)?(?:(?<![:\w])[@:](?<name>\w+)(?!\w)|\$(?<ordinal>\d+)(?!\d))(?(wrapped)\s*\])", RegexOptions.IgnoreCase)]
	private static partial Regex PlaceholderRegex();

	private static string GetPostgreSqlTypeName(DbParameter parameter)
	{
		NpgsqlDbType npgsqlDbType = parameter is NpgsqlParameter npgsqlParameter ? npgsqlParameter.NpgsqlDbType : InferNpgsqlDbType(parameter.Value);

		return npgsqlDbType switch
		{
			_ when npgsqlDbType.HasFlag(NpgsqlDbType.Array) => $"{(npgsqlDbType & ~NpgsqlDbType.Array).ToString().ToLowerInvariant()}[]",
			NpgsqlDbType.Varchar or NpgsqlDbType.Char => $"{npgsqlDbType.ToString().ToLowerInvariant()}({(parameter.Size <= 0 ? "unbounded" : parameter.Size.ToString(CultureInfo.InvariantCulture))})",
			NpgsqlDbType.Numeric => $"numeric({(parameter.Precision == 0 ? 18 : parameter.Precision)},{parameter.Scale})",
			_ => npgsqlDbType.ToString().ToLowerInvariant()
		};
	}

	private static NpgsqlDbType InferNpgsqlDbType(object? value) => value switch
	{
		long => NpgsqlDbType.Bigint,
		int => NpgsqlDbType.Integer,
		short => NpgsqlDbType.Smallint,
		byte => NpgsqlDbType.Smallint,
		bool => NpgsqlDbType.Boolean,
		decimal => NpgsqlDbType.Numeric,
		double or float => NpgsqlDbType.Double,
		DateTime => NpgsqlDbType.Timestamp,
		DateTimeOffset => NpgsqlDbType.TimestampTz,
		Guid => NpgsqlDbType.Uuid,
		byte[] => NpgsqlDbType.Bytea,
		DateOnly => NpgsqlDbType.Date,
		TimeOnly => NpgsqlDbType.Time,
		TimeSpan => NpgsqlDbType.Interval,
		System.Collections.IEnumerable and not string => NpgsqlDbType.Array | NpgsqlDbType.Text,
		_ => NpgsqlDbType.Varchar
	};

	private static string FormatSqlLiteral(object? value)
	{
		if (value is null || value == DBNull.Value)
		{
			return "NULL";
		}

		return value switch
		{
			string s => $"'{s.Replace("'", "''")}'",
			bool b => b ? "TRUE" : "FALSE",
			DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.ffffff}'::timestamp",
			DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss.ffffffzzz}'::timestamptz",
			DateOnly d => $"'{d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}'::date",
			TimeOnly t => $"'{t.ToString("HH:mm:ss.ffffff", CultureInfo.InvariantCulture)}'::time",
			TimeSpan ts => $"'{ts.ToString("c", CultureInfo.InvariantCulture)}'::interval",
			Enum e => Convert.ToString(Convert.ChangeType(e, e.GetTypeCode(), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)!,
			Guid g => $"'{g}'::uuid",
			byte[] bytes => $"'\\x{Convert.ToHexString(bytes).ToLowerInvariant()}'::bytea",
			decimal or double or float or long or int or short or byte => Convert.ToString(value, CultureInfo.InvariantCulture)!,
			System.Collections.IEnumerable items => FormatArrayLiteral(items),
			_ => $"'{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("'", "''")}'"
		};
	}

	private static string FormatArrayLiteral(System.Collections.IEnumerable items)
	{
		List<string> elements = [];
		foreach (object? item in items)
		{
			elements.Add(FormatSqlLiteral(item));
		}

		return elements.Count == 0 ? "'{}'" : $"ARRAY[{string.Join(", ", elements)}]";
	}
}
#endif
