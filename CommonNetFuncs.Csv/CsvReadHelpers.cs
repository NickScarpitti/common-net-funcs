using System.Data;
using System.Globalization;
using System.Reflection;
using CsvHelper;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Csv;

public static class CsvReadHelpers
{
    /// <summary>
    /// Read from physical CSV file
    /// </summary>
    /// <typeparam name="T">Type to read from rows</typeparam>
    /// <param name="filePath">Path to the CSV file to read from</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>List of T read from the CSV file</returns>
    public static List<T> ReadCsv<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        return ReadCsv<T>(reader, hasHeaders, cultureInfo);
    }

    /// <summary>
    /// Read from CSV file in a stream
    /// </summary>
    /// <typeparam name="T">Type to read from rows</typeparam>
    /// <param name="stream">Stream of a CSV file</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>List of T read from the CSV stream</returns>
    public static List<T> ReadCsv<T>(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(stream);
        return ReadCsv<T>(reader, hasHeaders, cultureInfo);
    }

    private static List<T> ReadCsv<T>(StreamReader reader, bool hasHeaders, CultureInfo? cultureInfo)
    {
        using CsvReader csv = new(reader, new CsvHelper.Configuration.CsvConfiguration(cultureInfo ?? CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaders
        });
        return csv.GetRecords<T>().ToList();
    }

    /// <summary>
    /// Read from physical CSV file
    /// </summary>
    /// <typeparam name="T">Type to read from rows</typeparam>
    /// <param name="filePath">Path to the CSV file to read from</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>List of T read from the CSV file</returns>
    public static async Task<List<T>> ReadCsvAsync<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        List<T> records = new();
        await foreach (T record in ReadCsvAsyncEnumerable<T>(reader, hasHeaders, cultureInfo))
        {
            records.Add(record);
        }
        return records;
    }

    /// <summary>
    /// Read from CSV file in a stream
    /// </summary>
    /// <typeparam name="T">Type to read from rows</typeparam>
    /// <param name="stream">Stream of a CSV file</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>List of T read from the CSV stream</returns>
    public static async Task<List<T>> ReadCsvAsync<T>(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(stream);
        List<T> records = new();
        await foreach (T record in ReadCsvAsyncEnumerable<T>(reader, hasHeaders, cultureInfo))
        {
            records.Add(record);
        }
        return records;
    }

    /// <summary>
    /// Read from physical CSV file
    /// </summary>
    /// <typeparam name="T">Type to read from rows</typeparam>
    /// <param name="filePath">Path to the CSV file to read from</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>Enumerated results of T read from the CSV file</returns>
    public static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        await foreach (T record in ReadCsvAsyncEnumerable<T>(reader, hasHeaders, cultureInfo))
        {
            yield return record;
        }
    }

    /// <summary>
    /// Read from CSV file in a stream
    /// </summary>
    /// <typeparam name="T">Type to read from rows</typeparam>
    /// <param name="stream">Stream of a CSV file</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>Enumerated results of T read from the CSV stream</returns>
    public static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(stream);
        await foreach (T record in ReadCsvAsyncEnumerable<T>(reader, hasHeaders, cultureInfo))
        {
            yield return record;
        }
    }

    private static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(StreamReader reader, bool hasHeaders, CultureInfo? cultureInfo)
    {
        using CsvReader csv = new(reader, new CsvHelper.Configuration.CsvConfiguration(cultureInfo ?? CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaders
        });

        await foreach (T record in csv.GetRecordsAsync<T>())
        {
            yield return record;
        }
    }

    /// <summary>
    /// Read from physical CSV file into a DataTable.
    /// </summary>
    /// <param name="filePath">Path to the CSV file to read from.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>DataTable populated from the CSV file.</returns>
    public static DataTable ReadCsvToDataTable(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        return ReadCsvToDataTable(reader, hasHeaders, null, cultureInfo);
    }

    /// <summary>
    /// Read from CSV file in a stream into a DataTable.
    /// </summary>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>DataTable populated from the CSV stream.</returns>
    public static DataTable ReadCsvToDataTable(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(stream);
        return ReadCsvToDataTable(reader, hasHeaders, null, cultureInfo);
    }

    /// <summary>
    /// Read from physical CSV file into a DataTable, using a specified data type for columns.
    /// </summary>
    /// <param name="filePath">Path to the CSV file to read from.</param>
    /// <param name="dataType">Type to use for DataTable columns.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>DataTable populated from the CSV file.</returns>
    public static DataTable ReadCsvToDataTable(string filePath, Type dataType, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        return ReadCsvToDataTable(reader, hasHeaders, dataType, cultureInfo);
    }

    /// <summary>
    /// Read from CSV file in a stream into a DataTable, using a specified data type for columns.
    /// </summary>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="dataType">Type to use for DataTable columns.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>DataTable populated from the CSV stream.</returns>
    public static DataTable ReadCsvToDataTable(Stream stream, Type dataType, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(stream);
        return ReadCsvToDataTable(reader, hasHeaders, dataType, cultureInfo);
    }

    private static DataTable ReadCsvToDataTable(StreamReader reader, bool hasHeaders, Type? dataType, CultureInfo? cultureInfo)
    {
        using CsvReader csv = new(reader, new CsvHelper.Configuration.CsvConfiguration(cultureInfo ?? CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaders
        });

        DataTable dataTable = new();
        if (dataType != null)
        {
            foreach (PropertyInfo prop in GetOrAddPropertiesFromReflectionCache(dataType))
            {
                dataTable.Columns.Add(prop.Name, prop.PropertyType);
            }
        }
        using CsvDataReader dataReader = new(csv);
        dataTable.Load(dataReader);

        return dataTable;
    }
}
