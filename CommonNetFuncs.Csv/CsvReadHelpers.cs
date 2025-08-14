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
    /// Read values from physical CSV file.
    /// </summary>
    /// <typeparam name="T">Type to read from rows in CSV file.</typeparam>
    /// <param name="filePath">Path to the CSV file to read from.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <returns><see cref="List{T}"/> read from the CSV file</returns>
    public static List<T> ReadCsv<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(filePath);
        return ReadCsv<T>(reader, hasHeaders, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read values from a CSV file contained in a <see cref="Stream"/>.
    /// </summary>
    /// <typeparam name="T">Type to read from rows in CSV file.</typeparam>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is <see langword="true"/>.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <returns><see cref="List{T}"/> of T read from the CSV <see cref="Stream"/></returns>
    public static List<T> ReadCsv<T>(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(stream, bufferSize: bufferSize);
        return ReadCsv<T>(reader, hasHeaders, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read values from a CSV file using a <see cref="StreamReader"/>.
    /// </summary>
    /// <typeparam name="T">Type to read from rows in CSV file.</typeparam>
    /// <param name="reader">StreamReader to read from.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is <see langword="true"/>.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <returns><see cref="List{T}"/> of T read from the CSV <see cref="StreamReader"/></returns>
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
    /// Read values from physical CSV file.
    /// </summary>
    /// <typeparam name="T">Type to read from rows in CSV file.</typeparam>
    /// <param name="filePath">Path to the CSV file to read from.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is <see langword="true"/>.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns><see cref="List{T}"/> read from the CSV file.</returns>
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
    /// Read values asynchronously from CSV file in a <see cref="Stream"/>.
    /// </summary>
    /// <typeparam name="T">Type to read from rows in CSV file.</typeparam>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is <see langword="true"/>.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns><see cref="List{T}"/> read from the CSV <see cref="Stream"/>.</returns>
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
    /// Read values asynchronously from physical CSV file
    /// </summary>
    /// <typeparam name="T">Type to read from rows in CSV file.</typeparam>
    /// <param name="filePath">Path to the CSV file to read from.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is <see langword="true"/>.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns><see cref="IAsyncEnumerable{T}"/> containing the values read from the CSV file.</returns>
    public static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
        await foreach (T record in ReadCsvAsyncEnumerable<T>(fileStream, hasHeaders, cultureInfo, bufferSize, cancellationToken))
        {
            yield return record;
        }
    }

    /// <summary>
    /// Read values from CSV file in a <see cref="Stream"/> asynchronously.
    /// </summary>
    /// <typeparam name="T">Type to read from rows in CSV file.</typeparam>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is <see langword="true"/>.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    ///  <returns><see cref="IAsyncEnumerable{T}"/> containing the values read from the CSV <see cref="Stream"/>.</returns>
    public static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new(stream, bufferSize: bufferSize);
        await foreach (T record in ReadCsvAsyncEnumerable<T>(reader, hasHeaders, cultureInfo, bufferSize, cancellationToken))
        {
            yield return record;
        }
    }

    /// <summary>
    /// Read values from CSV file in a <see cref="StreamReader"/> asynchronously.
    /// </summary>
    /// <typeparam name="T">Type to read from rows in CSV file.</typeparam>
    /// <param name="reader">StreamReader for the CSV file.</param>
    /// <param name="hasHeaders">Indicates if the CSV file has headers.</param>
    /// <param name="cultureInfo">CultureInfo to use for parsing.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns><see cref="IAsyncEnumerable{T}"/> containing the values read from the CSV in the <see cref="StreamReader"/>.</returns>
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
    /// Read values from physical CSV file into a <see cref="DataTable"/>.
    /// </summary>
    /// <param name="filePath">Path to the CSV file to read from.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <returns><see cref="DataTable"/> populated from the values in the CSV file.</returns>
    public static DataTable ReadCsvToDataTable(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(filePath);
        return ReadCsvToDataTable(reader, hasHeaders, null, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read values from CSV file in a <see cref="Stream"/> into a <see cref="DataTable"/>.
    /// </summary>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <returns><see cref="DataTable"/> populated by the values in the CSV <see cref="Stream"/>.</returns>
    public static DataTable ReadCsvToDataTable(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(stream, bufferSize: bufferSize);
        return ReadCsvToDataTable(reader, hasHeaders, null, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read values from physical CSV file into a <see cref="DataTable"/>, using a specified data type for columns.
    /// </summary>
    /// <param name="filePath">Path to the CSV file to read from.</param>
    /// <param name="dataType">Type to use for DataTable columns.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <returns><see cref="DataTable"/> populated by the values in CSV file.</returns>
    public static DataTable ReadCsvToDataTable(string filePath, Type dataType, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(filePath);
        return ReadCsvToDataTable(reader, hasHeaders, dataType, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read values from CSV file in a <see cref="Stream"/> into a <see cref="DataTable"/>, using a specified data type for columns.
    /// </summary>
    /// <param name="stream">Stream of a CSV file.</param>
    /// <param name="dataType">Type to use for DataTable columns.</param>
    /// <param name="hasHeaders">Optional: Indicates file has headers. Default is true.</param>
    /// <param name="cultureInfo">Optional: Culture to read file with. Default is invariant culture.</param>
    /// <param name="bufferSize">Optional: Size of the buffer to use when reading the file. Default is 4096 bytes.</param>
    /// <returns><see cref="DataTable"/> populated from the CSV <see cref="Stream"/>.</returns>
    public static DataTable ReadCsvToDataTable(Stream stream, Type dataType, bool hasHeaders = true, CultureInfo? cultureInfo = null, int bufferSize = 4096)
    {
        using StreamReader reader = new(stream, bufferSize: bufferSize);
        return ReadCsvToDataTable(reader, hasHeaders, dataType, cultureInfo, bufferSize);
    }

    /// <summary>
    /// Read values from CSV file in a <see cref="StreamReader"/> into a <see cref="DataTable"/>, using a specified data type for columns.
    /// </summary>
    /// <param name="reader">StreamReader for the CSV file.</param>
    /// <param name="hasHeaders">Indicates if the CSV file has headers.</param>
    /// <param name="dataType">Type to use for DataTable columns.</param>
    /// <param name="cultureInfo">CultureInfo to use for parsing.</param>
    /// <param name="bufferSize">Size of the buffer to use when reading the file.</param>
    /// <returns><see cref="DataTable"/> populated by the values in the CSV <see cref="StreamReader"/>.</returns>
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
