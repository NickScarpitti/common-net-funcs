using System.Data;
using System.Globalization;
using CsvHelper;
using static System.Convert;
using static CommonNetFuncs.Core.Streams;

namespace CommonNetFuncs.Csv;

public static class CsvHelperExportHelpers
{
    /// <summary>
    /// Writes collection of values to a CSV file in a MemoryStream
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="dataList">Data to be inserted into the CSV file</param>
    /// <param name="memoryStream">Stream to contain the CSV file data</param>
    /// <returns>MemoryStream containing the CSV file data</returns>
    public static async Task<MemoryStream> ExportListToCsv<T>(this IEnumerable<T> dataList, MemoryStream? memoryStream = null)
    {
        memoryStream ??= new();
        await using StreamWriter streamWriter = new(memoryStream);
        await using CsvWriter csvWriter = new(streamWriter, CultureInfo.InvariantCulture);
        await csvWriter.WriteRecordsAsync(dataList);
        return memoryStream;
    }

    /// <summary>
    /// Writes contents of a DataTable object to a CSV file in a MemoryStream
    /// </summary>
    /// <param name="dataTable">Data to be inserted into the CSV file</param>
    /// <param name="memoryStream">Stream to contain the CSV file data</param>
    /// <returns>MemoryStream containing the CSV file data</returns>
    public static async Task<MemoryStream> ExportListToCsv(this DataTable dataTable, MemoryStream? memoryStream = null)
    {
        memoryStream ??= new();
        await using MemoryStream sourceMemoryStream = new();
        await using StreamWriter streamWriter = new(sourceMemoryStream);
        //Headers
        for (int i = 0; i < dataTable.Columns.Count; i++)
        {
            await streamWriter.WriteAsync(dataTable.Columns[i]?.ToString() ?? string.Empty);
            if (i < dataTable.Columns.Count - 1)
            {
                await streamWriter.WriteAsync(",");
            }
        }
        await streamWriter.WriteAsync(streamWriter.NewLine);
        foreach (DataRow row in dataTable.Rows)
        {
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                if (!IsDBNull(row[i]))
                {
                    string? value = row[i].ToString();
                    if (value?.Contains(',') ?? false)
                    {
                        value = string.Format("\"{0}\"", value);
                        await streamWriter.WriteAsync(value);
                    }
                    else
                    {
                        await streamWriter.WriteAsync(row[i]?.ToString() ?? string.Empty);
                    }
                }
                if (i < dataTable.Columns.Count - 1)
                {
                    await streamWriter.WriteAsync(",");
                }
            }
            await streamWriter.WriteAsync(streamWriter.NewLine);
        }

        await streamWriter.FlushAsync();
        sourceMemoryStream.Position = 0;
        await memoryStream.WriteStreamToStream(sourceMemoryStream);

        return memoryStream;
    }
}
