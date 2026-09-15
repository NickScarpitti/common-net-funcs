using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace CommonNetFuncs.Sql.PostgreSql;

#if NET5_0_OR_GREATER
public static class Debug
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
			int ordinal = 0;
			foreach (DbParameter p in cmd.Parameters)
			{
				string name = p.ParameterName?.TrimStart('@', ':') ?? string.Empty;
				string literal = FormatSqlLiteral(p.Value);
				string label = !string.IsNullOrEmpty(name) ? $"@{name}" : $"${ordinal + 1}";
				script.AppendLine($"-- {label} [{GetPostgreSqlTypeName(p)}] = {literal}");

				commandText = SubstituteParameter(commandText, name, ordinal, literal);
				ordinal++;
			}
			script.AppendLine();
		}

		script.AppendLine("-- ==== Command ====");
		script.AppendLine(commandText);

		return script.ToString();
	}

	private static string SubstituteParameter(string commandText, string name, int ordinal, string literal)
	{
		string result = commandText;
		if (!string.IsNullOrEmpty(name))
		{
			result = Regex.Replace(result, $@"[@:]{Regex.Escape(name)}(?!\w)", literal.Replace("$", "$$"));
		}

		result = Regex.Replace(result, $@"\${ordinal + 1}(?!\d)", literal.Replace("$", "$$"));

		return result;
	}

	private static string GetPostgreSqlTypeName(DbParameter parameter)
	{
		NpgsqlDbType npgsqlDbType = parameter is NpgsqlParameter npgsqlParameter ? npgsqlParameter.NpgsqlDbType : InferNpgsqlDbType(parameter.Value);

		return npgsqlDbType switch
		{
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
			Guid g => $"'{g}'::uuid",
			byte[] bytes => $"'\\x{Convert.ToHexString(bytes).ToLowerInvariant()}'::bytea",
			decimal or double or float or long or int or short or byte => Convert.ToString(value, CultureInfo.InvariantCulture)!,
			_ => $"'{value.ToString()?.Replace("'", "''")}'"
		};
	}
}
#endif
