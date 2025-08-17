using System.Data;
using System.Globalization;
using System.Reflection;
using CsvHelper;

namespace CommonNetFuncs.Csv;

public static class CsvReadHelpers
{
    public static List<T> ReadCsv<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        return ReadCsv<T>(reader, hasHeaders, cultureInfo);
    }

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

    public static async IAsyncEnumerable<T> ReadCsvAsyncEnumerable<T>(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        await foreach (T record in ReadCsvAsyncEnumerable<T>(reader, hasHeaders, cultureInfo))
        {
            yield return record;
        }
    }

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

    public static DataTable ReadCsvToDataTable(string filePath, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        return ReadCsvToDataTable(reader, hasHeaders, null, cultureInfo);
    }

    public static DataTable ReadCsvToDataTable(Stream stream, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(stream);
        return ReadCsvToDataTable(reader, hasHeaders, null, cultureInfo);
    }

    public static DataTable ReadCsvToDataTable(string filePath, Type dataType, bool hasHeaders = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        return ReadCsvToDataTable(reader, hasHeaders, dataType, cultureInfo);
    }

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
            foreach (PropertyInfo prop in dataType.GetProperties())
            {
                dataTable.Columns.Add(prop.Name, prop.PropertyType);
            }
        }
        using CsvDataReader dataReader = new(csv);
        dataTable.Load(dataReader);

        return dataTable;
    }
}
