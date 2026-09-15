using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;

namespace CommonNetFuncs.Sql.SqlServer;

#if NET5_0_OR_GREATER
public static class Debug
{
	/// <summary>
	/// Renders a <see cref="DbCommand"/> as a self-contained T-SQL script that can be pasted
	/// directly into SSMS / Azure Data Studio and executed with all parameters pre-declared.
	/// FOR DIAGNOSTICS ONLY - the produced text is not safe to execute as user input.
	/// </summary>
	public static string RenderCommandAsScript(DbCommand cmd)
	{
		StringBuilder script = new();

		if (cmd.Parameters.Count > 0)
		{
			script.AppendLine("-- ==== Parameters ====");
			foreach (DbParameter p in cmd.Parameters)
			{
				string name = p.ParameterName.StartsWith('@') ? p.ParameterName : "@" + p.ParameterName;
				script.AppendLine($"DECLARE {name} {GetSqlTypeDeclaration(p)} = {FormatSqlLiteral(p.Value)};");
			}
			script.AppendLine();
		}

		script.AppendLine("-- ==== Command ====");
		script.AppendLine(cmd.CommandText);

		return script.ToString();
	}

	private static string GetSqlTypeDeclaration(DbParameter parameter)
	{
		SqlDbType sqlDbType = parameter is SqlParameter sqlParameter
			? sqlParameter.SqlDbType
			: InferSqlDbType(parameter.Value);

		return sqlDbType switch
		{
			SqlDbType.NVarChar or SqlDbType.VarChar or SqlDbType.Char or SqlDbType.NChar =>
				$"{sqlDbType.ToString().ToUpperInvariant()}({(parameter.Size <= 0 ? "MAX" : parameter.Size.ToString())})",
			SqlDbType.Decimal => $"DECIMAL({(parameter.Precision == 0 ? 18 : parameter.Precision)},{parameter.Scale})",
			_ => sqlDbType.ToString().ToUpperInvariant()
		};
	}

	private static SqlDbType InferSqlDbType(object? value) => value switch
	{
		long => SqlDbType.BigInt,
		int => SqlDbType.Int,
		short => SqlDbType.SmallInt,
		byte => SqlDbType.TinyInt,
		bool => SqlDbType.Bit,
		decimal => SqlDbType.Decimal,
		double or float => SqlDbType.Float,
		DateTime => SqlDbType.DateTime2,
		DateTimeOffset => SqlDbType.DateTimeOffset,
		Guid => SqlDbType.UniqueIdentifier,
		byte[] => SqlDbType.VarBinary,
		_ => SqlDbType.NVarChar
	};

	private static string FormatSqlLiteral(object? value)
	{
		if (value is null || value == DBNull.Value)
		{
			return "NULL";
		}

		return value switch
		{
			string s => $"N'{s.Replace("'", "''")}'",
			bool b => b ? "1" : "0",
			DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
			DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss.fff zzz}'",
			Guid g => $"'{g}'",
			byte[] bytes => "0x" + Convert.ToHexString(bytes),
			decimal or double or float or long or int or short or byte =>
				Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!,
			_ => $"N'{value.ToString()?.Replace("'", "''")}'"
		};
	}
}
#endif
