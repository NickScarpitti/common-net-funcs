using CommonNetCoreFuncs.Conversion;
using CommonNetCoreFuncs.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonNetCoreFuncs.Web;

/// <summary>
/// For use with the DataTables JavaScript framework
/// </summary>
public static class DataTableHelpers
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static DataTableRequest GetDataTableRequest(HttpRequest request)
    {
        DataTableRequest dataTableRequest = new();
        try
        {
            dataTableRequest.Draw = request.Form.Where(x => x.Key.StrEq("draw")).Select(x => x.Value).FirstOrDefault();

            int i = 0;

            while (request.Form.Where(y => y.Key.StrEq($"order[{i}][column]")).Any())
            {
                dataTableRequest.SortColumns.Add(i, request.Form.Where(x => x.Key.StrEq("columns[" + request.Form.Where(y => y.Key.StrEq($"order[{i}][column]"))
                .Select(z => z.Value).FirstOrDefault() + "][data]")).Select(x => x.Value).FirstOrDefault());

                dataTableRequest.SortColumnDir.Add(i, request.Form.Where(x => x.Key.StrEq($"order[{i}][dir]")).Select(x => x.Value).FirstOrDefault());
                i++;
            }

            string start = request.Form.Where(x => x.Key.StrEq("start")).Select(x => x.Value).FirstOrDefault();
            string length = request.Form.Where(x => x.Key.StrEq("length")).Select(x => x.Value).FirstOrDefault();
            string searchValue = request.Form.Where(x => x.Key.StrEq("search[value]")).Select(x => x.Value).FirstOrDefault();

            //Paging Size (10,20,50,100)
            dataTableRequest.PageSize = length != StringValues.Empty ? Convert.ToInt32(length) : 0;
            dataTableRequest.Skip = start != StringValues.Empty ? Convert.ToInt32(start) : 0;

            //Get search value key pairs
            if (!string.IsNullOrEmpty(searchValue))
            {
                List<string> vals = searchValue.ToString().Split(",").ToList();
                foreach (string val in vals)
                {
                    string cleanVal = val.CleanQueryParam()!;
                    int startPos = cleanVal.IndexOf("=") + 1;
                    if (startPos == 0)
                    {
                        continue;
                    }
                    string key = cleanVal[..(startPos - 1)];
                    if (startPos + 1 <= cleanVal.Length)
                    {
                        int valLength = cleanVal.Length - (startPos);
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
            logger.Error(ex, "GetDataTableRequest Error");
        }
        return dataTableRequest;
    }

    public static SortAndLimitPostModel GetSortAndLimitPostModel(DataTableRequest request)
    {
        return new SortAndLimitPostModel()
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
        SearchValues = new();
        SortColumnDir = new();
        SortColumns = new();
    }

    public int PageSize { get; set; }
    public int Skip { get; set; }
    public Dictionary<int, string?> SortColumnDir { get; set; }
    public Dictionary<int, string?> SortColumns { get; set; }
    public string? Draw { get; set; }
    public Dictionary<string, string?> SearchValues { get; set; }
}

public class SortAndLimitPostModel
{
    public SortAndLimitPostModel()
    {
        SortColumns = new();
        SortColumnDir = new();
    }
    public Dictionary<int, string?> SortColumns { get; set; }
    public Dictionary<int, string?> SortColumnDir { get; set; }
    public int Skip { get; set; }
    public int PageSize { get; set; }
}

#endregion
