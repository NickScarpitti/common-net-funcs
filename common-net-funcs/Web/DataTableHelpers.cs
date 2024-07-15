using MemoryPack;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NLog;
using static System.Convert;
using static Common_Net_Funcs.Tools.DebugHelpers;
using static Common_Net_Funcs.Tools.StringHelpers;

namespace Common_Net_Funcs.Web;

/// <summary>
/// For use with the DataTables JavaScript framework
/// </summary>
public static class DataTableHelpers
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Parse the DataTables HttpRequest object into the DataTableRequest class
    /// </summary>
    /// <param name="request">HTTP request sent by DataTables</param>
    /// <returns>DataTableRequest object containing the parsed request values</returns>
    public static DataTableRequest GetDataTableRequest(HttpRequest request)
    {
        DataTableRequest dataTableRequest = new();
        try
        {
            dataTableRequest.Draw = request.Form.Where(x => x.Key.StrEq("draw")).Select(x => x.Value).FirstOrDefault();

            for (int i = 0; request.Form.Any(y => y.Key.StrEq($"order[{i}][column]")); i++)
            {
                dataTableRequest.SortColumns.Add(i, request.Form.Where(x => x.Key.StrEq("columns[" + request.Form.Where(y => y.Key.StrEq($"order[{i}][column]"))
                    .Select(z => z.Value).FirstOrDefault() + "][data]")).Select(x => x.Value).FirstOrDefault());

                dataTableRequest.SortColumnDir.Add(i, request.Form.Where(x => x.Key.StrEq($"order[{i}][dir]")).Select(x => x.Value).FirstOrDefault());
            }

            string? start = request.Form.Where(x => x.Key.StrEq("start")).Select(x => x.Value).FirstOrDefault();
            string? length = request.Form.Where(x => x.Key.StrEq("length")).Select(x => x.Value).FirstOrDefault();
            string? searchValue = request.Form.Where(x => x.Key.StrEq("search[value]")).Select(x => x.Value).FirstOrDefault();

            //Paging Size (10,20,50,100)
            dataTableRequest.PageSize = length != StringValues.Empty ? ToInt32(length) : 0;
            dataTableRequest.Skip = start != StringValues.Empty ? ToInt32(start) : 0;

            //Get search value key pairs
            if (!searchValue.IsNullOrEmpty())
            {
                foreach (string val in searchValue.Split(',').ToList())
                {
                    string cleanVal = val.CleanQueryParam()!;
                    int startPos = cleanVal.IndexOf('=') + 1;
                    if (startPos == 0)
                    {
                        continue;
                    }
                    string key = cleanVal[..(startPos - 1)];
                    if (startPos + 1 <= cleanVal.Length)
                    {
                        int valLength = cleanVal.Length - startPos;
                        string outVal = cleanVal.Substring(startPos, valLength);
                        dataTableRequest.SearchValues.Add(key, outVal);
                    }
                    else
                    {
                        dataTableRequest.SearchValues.Add(key, null);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
        return dataTableRequest;
    }

    /// <summary>
    /// Transform DataTableRequest into an object that can be used to limit the results sent back from a query why using paging or applying a sort
    /// </summary>
    /// <param name="request">DataTableRequest to get SortAndLimitPostModel object for</param>
    /// <returns>SortAndLimitPostModel object created from the parameters in DataTableRequest</returns>
    public static SortAndLimitPostModel GetSortAndLimitPostModel(DataTableRequest request)
    {
        return new()
        {
            SortColumns = request.SortColumns,
            SortColumnDir = request.SortColumnDir,
            Skip = request.Skip,
            PageSize = request.PageSize
        };
    }
}

#region Classes

public class DataTableRequest
{
    public DataTableRequest()
    {
        SearchValues = [];
        SortColumnDir = [];
        SortColumns = [];
    }

    public int PageSize { get; set; }
    public int Skip { get; set; }
    public Dictionary<int, string?> SortColumnDir { get; set; }
    public Dictionary<int, string?> SortColumns { get; set; }
    public string? Draw { get; set; }
    public Dictionary<string, string?> SearchValues { get; set; }
}

[MemoryPackable]
[MessagePackObject(true)]
public partial class SortAndLimitPostModel
{
    public SortAndLimitPostModel()
    {
        SortColumns = [];
        SortColumnDir = [];
    }
    public Dictionary<int, string?> SortColumns { get; set; }
    public Dictionary<int, string?> SortColumnDir { get; set; }
    public int Skip { get; set; }
    public int PageSize { get; set; }
}

#endregion Classes
