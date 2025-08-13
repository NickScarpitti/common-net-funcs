using System.Data;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    /// <returns><see cref="List{T}"/> of T read from the CSV file</returns>
    public static List<T> ReadCsv<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(filePath);
        return ReadCsv<T>(reader, hasHeaders, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read from CSV file in a stream
    /// </summary>
    /// <typeparam name="T">Type to read from rows</typeparam>
    /// <param name="stream">Stream of a CSV file</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns><see cref="List{T}"/> of T read from the CSV stream</returns>
    public static List<T> ReadCsv<T>(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(stream, bufferSize: bufferSize);
        return ReadCsv<T>(reader, hasHeaders, cultureInfo, bufferSize);
    }

    private static List<T> ReadCsv<T>(StreamReader reader, bool hasHeaders, CultureInfo? cultureInfo, int bufferSize)
    {
        using CsvReader csv = new(reader, new CsvHelper.Configuration.CsvConfiguration(cultureInfo ?? CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaders,
            BufferSize = bufferSize
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
    /// <returns><see cref="List{T}"/> of T read from the CSV file</returns>
    public static async Task<List<T>> ReadCsvAsync<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new(filePath);
        List<T> records = new();
        await foreach (T record in ReadCsvAsyncEnumerable<T>(reader, hasHeaders, cultureInfo, bufferSize, cancellationToken))
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
    /// <returns><see cref="List{T}"/> of T read from the CSV stream</returns>
    public static async Task<List<T>> ReadCsvAsync<T>(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, CancellationToken cancellationToken = default)
    {
        List<T> records = new();
        await foreach (T record in ReadCsvAsyncEnumerable<T>(stream, hasHeaders, cultureInfo, bufferSize, cancellationToken))
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
    public static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
        await foreach (T record in ReadCsvAsyncEnumerable<T>(fileStream, hasHeaders, cultureInfo, bufferSize, cancellationToken))
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
    public static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new(stream, bufferSize: bufferSize);
        await foreach (T record in ReadCsvAsyncEnumerable<T>(reader, hasHeaders, cultureInfo, bufferSize, cancellationToken))
        {
            yield return record;
        }
    }

    private static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(StreamReader reader, bool hasHeaders, CultureInfo? cultureInfo, int bufferSize = 4096, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using CsvReader csv = new(reader, new CsvHelper.Configuration.CsvConfiguration(cultureInfo ?? CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaders,
            BufferSize = bufferSize
        });

        await foreach (T record in csv.GetRecordsAsync<T>(cancellationToken))
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
    public static DataTable ReadCsvToDataTable(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(filePath);
        return ReadCsvToDataTable(reader, hasHeaders, null, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read from CSV file in a stream into a DataTable.
    /// </summary>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>DataTable populated from the CSV stream.</returns>
    public static DataTable ReadCsvToDataTable(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(stream, bufferSize: bufferSize);
        return ReadCsvToDataTable(reader, hasHeaders, null, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read from physical CSV file into a DataTable, using a specified data type for columns.
    /// </summary>
    /// <param name="filePath">Path to the CSV file to read from.</param>
    /// <param name="dataType">Type to use for DataTable columns.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>DataTable populated from the CSV file.</returns>
    public static DataTable ReadCsvToDataTable(string filePath, Type dataType, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(filePath);
        return ReadCsvToDataTable(reader, hasHeaders, dataType, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read from CSV file in a stream into a DataTable, using a specified data type for columns.
    /// </summary>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="dataType">Type to use for DataTable columns.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <returns>DataTable populated from the CSV stream.</returns>
    public static DataTable ReadCsvToDataTable(Stream stream, Type dataType, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(stream, bufferSize: bufferSize);
        return ReadCsvToDataTable(reader, hasHeaders, dataType, cultureInfo, bufferSize);
    }

    private static DataTable ReadCsvToDataTable(StreamReader reader, bool hasHeaders, Type? dataType, CultureInfo? cultureInfo, int bufferSize)
    {
        using CsvReader csv = new(reader, new CsvHelper.Configuration.CsvConfiguration(cultureInfo ?? CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeaders,
            BufferSize = bufferSize
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
