using System.Data;
using System.Globalization;
using Common_Net_Funcs.Conversion;
using CsvHelper;

namespace Common_Net_Funcs.Excel;
public static class CsvHelperExportHelpers
{
    public static async Task<MemoryStream?> ExportListToCsv<T>(List<T> dataList, MemoryStream? memoryStream = null)
    {
        memoryStream ??= new();
        using StreamWriter streamWriter = new StreamWriter(memoryStream);
        using CsvWriter csvWriter = new(streamWriter, CultureInfo.InvariantCulture);
        await csvWriter.WriteRecordsAsync(dataList);
        return memoryStream;
    }

    public static async Task<MemoryStream?> ExportListToCsv(DataTable dataTable, MemoryStream? memoryStream = null)
    {
        memoryStream ??= new();
        using MemoryStream sourceMemoryStream = new();
        using StreamWriter streamWriter = new StreamWriter(sourceMemoryStream);
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
                if (!Convert.IsDBNull(row[i]))
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

    public static async Task WriteFileToMemoryStreamAsync(this MemoryStream memoryStream, MemoryStream sourceMmoryStream)
    {
        using MemoryStream tempStream = new();

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
