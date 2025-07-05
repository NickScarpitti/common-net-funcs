using System.Data;
using System.Globalization;
using CsvHelper;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Core.Streams;
using static System.Convert;

namespace CommonNetFuncs.Csv;

public static class CsvExportHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Writes collection of values to a CSV file in a MemoryStream
    /// </summary>
    /// <typeparam name="T">Object type</typeparam>
    /// <param name="dataList">Data to be inserted into the CSV file</param>
    /// <param name="memoryStream">Stream to contain the CSV file data</param>
    /// <returns>MemoryStream containing the CSV file data</returns>
    public static async Task<MemoryStream> ExportToCsv<T>(this IEnumerable<T> dataList, MemoryStream? memoryStream = null, CancellationToken cancellationToken = default)
    {
        memoryStream ??= new();
        await using StreamWriter streamWriter = new(memoryStream, leaveOpen: true);
        await using CsvWriter csvWriter = new(streamWriter, CultureInfo.InvariantCulture);
        try
        {
            await csvWriter.WriteRecordsAsync(dataList, cancellationToken).ConfigureAwait(false);
            await csvWriter.FlushAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Writes contents of a DataTable object to a CSV file in a MemoryStream
    /// </summary>
    /// <param name="dataTable">Data to be inserted into the CSV file</param>
    /// <param name="memoryStream">Stream to contain the CSV file data</param>
    /// <returns>MemoryStream containing the CSV file data</returns>
    public static async Task<MemoryStream> ExportToCsv(this DataTable dataTable, MemoryStream? memoryStream = null, CancellationToken cancellationToken = default)
    {
        memoryStream ??= new();
        await using MemoryStream sourceMemoryStream = new();
        await using StreamWriter streamWriter = new(sourceMemoryStream);
        try
        {
            //Headers
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await streamWriter.WriteAsync(dataTable.Columns[i]?.ToString() ?? string.Empty).ConfigureAwait(false);
                if (i < dataTable.Columns.Count - 1)
                {
                    await streamWriter.WriteAsync(",").ConfigureAwait(false);
                }
            }
            await streamWriter.WriteAsync(streamWriter.NewLine).ConfigureAwait(false);
            foreach (DataRow row in dataTable.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    if (!IsDBNull(row[i]))
                    {
                        string? value = row[i].ToString();
                        if (value?.Contains(',') ?? false)
                        {
                            value = $"\"{value}\"";
                            await streamWriter.WriteAsync(value).ConfigureAwait(false);
                        }
                        else
                        {
                            await streamWriter.WriteAsync(row[i]?.ToString() ?? string.Empty).ConfigureAwait(false);
                        }
                    }
                    if (i < dataTable.Columns.Count - 1)
                    {
                        await streamWriter.WriteAsync(",").ConfigureAwait(false);
                    }
                }
                await streamWriter.WriteAsync(streamWriter.NewLine).ConfigureAwait(false);
            }

            await streamWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            sourceMemoryStream.Position = 0;
            await memoryStream.WriteStreamToStream(sourceMemoryStream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            streamWriter.Close();
            await streamWriter.DisposeAsync().ConfigureAwait(false);

            sourceMemoryStream.Close();
            await sourceMemoryStream.DisposeAsync().ConfigureAwait(false);
        }

        return memoryStream;
    }
}
