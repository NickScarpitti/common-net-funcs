﻿using System.Text.Json.Serialization;
using MemoryPack;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using static CommonNetFuncs.Sql.Common.QueryParameters;
using static CommonNetFuncs.Web.Common.ContentTypes;
using static System.Convert;

namespace CommonNetFuncs.Web.Interface;

/// <summary>
/// For use with the DataTables JavaScript framework
/// </summary>
public static class DataTableHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Parse the DataTables HttpRequest object into the DataTableRequest class
    /// </summary>
    /// <param name="request">HTTP request sent by DataTables</param>
    /// <returns><see cref="DataTable"/>Request object containing the parsed request values</returns>
    public static DataTableRequest GetDataTableRequest(this HttpRequest request)
    {
        DataTableRequest dataTableRequest = new();
        try
        {
            if (FormDataTypes.Any(x => request.ContentType?.Contains(x, StringComparison.InvariantCultureIgnoreCase) ?? false) && request.Form != null)
            {
                dataTableRequest.Draw = request.Form.Where(x => string.Equals(x.Key, "draw", StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault();

                for (int i = 0; request.Form.Any(y => string.Equals(y.Key, $"order[{i}][column]", StringComparison.InvariantCultureIgnoreCase)); i++)
                {
                    dataTableRequest.SortColumns.Add(i, request.Form.Where(x => string.Equals(x.Key, "columns[" + request.Form.Where(y => string.Equals(y.Key, $"order[{i}][column]", StringComparison.InvariantCultureIgnoreCase))
                        .Select(z => z.Value).FirstOrDefault() + "][data]", StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault());

                    dataTableRequest.SortColumnDir.Add(i, request.Form.Where(x => string.Equals(x.Key, $"order[{i}][dir]", StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault());
                }

                string? start = request.Form.Where(x => string.Equals(x.Key, "start", StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault();
                string? length = request.Form.Where(x => string.Equals(x.Key, "length", StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault();
                string? searchValue = request.Form.Where(x => string.Equals(x.Key, "search[value]", StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault();

                //Paging Size (10,20,50,100)
                dataTableRequest.PageSize = length != StringValues.Empty ? ToInt32(length) : 0;
                dataTableRequest.Skip = start != StringValues.Empty ? ToInt32(start) : 0;

                //Get search value key pairs
                if (!string.IsNullOrEmpty(searchValue))
                {
                    foreach (string val in searchValue.Split(','))
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
            else
            {
                logger.Warn($"Unable to read Datatable request: Body is not a valid form data type [{string.Join(", ", FormDataTypes)}], or form data is null");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{nameof(DataTableHelpers)}.{nameof(GetDataTableRequest)} Error");
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

public sealed class DataTableRequest
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

public sealed class DataTableReturnData<T>
{
    [JsonPropertyName("draw")]
    public string? Draw { get; set; }

    [JsonPropertyName("recordsFiltered")]
    public int? RecordsFiltered { get; set; }

    [JsonPropertyName("recordsTotal")]
    public int? RecordsTotal { get; set; }

    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = [];
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
