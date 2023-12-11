using System.Data;
using System.Globalization;
using CsvHelper;
using static System.Convert;
using static Common_Net_Funcs.Conversion.StringConversion;

namespace Common_Net_Funcs.Excel;

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
            await streamWriter.WriteAsync(dataTable.Columns[i].ToNString());
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
                        await streamWriter.WriteAsync(row[i].ToNString());
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
        sourceMemoryStream.Seek(0, SeekOrigin.Begin);
        await memoryStream.WriteFileToMemoryStreamAsync(sourceMemoryStream);

        return memoryStream;
    }

    /// <summary>
    /// Copy local MemoryStream to passed in MemoryStream
    /// </summary>
    /// <param name="memoryStream">MemoryStream to copy from</param>
    /// <param name="sourceMmoryStream">MemoryStream to copy to</param>
    public static async Task WriteFileToMemoryStreamAsync(this MemoryStream memoryStream, MemoryStream sourceMmoryStream)
    {
        await using MemoryStream tempStream = new();

        sourceMmoryStream.Seek(0, SeekOrigin.Begin);

        //wb.SaveAs(tempStream, options);
        await tempStream.WriteAsync(sourceMmoryStream.ToArray());
        await tempStream.FlushAsync();
        tempStream.Seek(0, SeekOrigin.Begin);
        await tempStream.CopyToAsync(memoryStream);
        await tempStream.DisposeAsync();
        await memoryStream.FlushAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);
    }
}
