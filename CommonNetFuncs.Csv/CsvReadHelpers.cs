using System.Globalization;
using CsvHelper;

namespace CommonNetFuncs.Csv;

public static class CsvReadHelpers
{
    public static List<T> ReadCsv<T>(string filePath, bool hasHeader = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(filePath);
        using CsvReader csv = new(reader, new CsvHelper.Configuration.CsvConfiguration(cultureInfo ?? CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeader
        });
        return csv.GetRecords<T>().ToList(); //Need to enumerate results before stream closure?
    }

    public static List<T> ReadCsvFromStream<T>(Stream stream, bool hasHeader = true, CultureInfo? cultureInfo = null)
    {
        using StreamReader reader = new(stream);
        using CsvReader csv = new(reader, new CsvHelper.Configuration.CsvConfiguration(cultureInfo ?? CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeader
        });
        return csv.GetRecords<T>().ToList(); //Need to enumerate results before stream closure?
    }
}
