﻿using System.Data;
using System.Text.RegularExpressions;
using static System.Web.HttpUtility;

namespace CommonNetFuncs.Email;

public static partial class HtmlEmailBuilder
{
    [GeneratedRegex(@"https?://[^\n\t< ]+", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex UrlRegex();

    /// <summary>
    /// Creates an HTML body for an email using the inputs provided
    /// </summary>
    /// <param name="body">Main text body of the email that goes before a table if there is one</param>
    /// <param name="footer">Any text to be displayed under the table or after the body</param>
    /// <param name="tableData">Data to be formatted into an HTML table</param>
    /// <returns>HTML Body of an email</returns>
    public static string BuildHtmlEmail(string body, string? footer = null, DataTable? tableData = null, CancellationToken cancellationToken = default)
    {
        string text = "<html><body>";
        text += body.StringtoHtml();
        text += tableData == null || tableData.Rows.Count == 0 ? string.Empty : "<br><br>";
        text += tableData.CreateHtmlTable(cancellationToken: cancellationToken);
        text += !string.IsNullOrWhiteSpace(footer) ? "<br><br>" : string.Empty;
        text += footer.StringtoHtml();
        text += "</body></html>";

        return text.FormatAllUrlsToHtml();
    }

    /// <summary>
    /// Creates an HTML body for an email using the inputs provided
    /// </summary>
    /// <param name="body">Main text body of the email that goes before a table if there is one</param>
    /// <param name="footer">Any text to be displayed under the table or after the body</param>
    /// <param name="tableData">Data to be formatted into a table. First item should be the header data</param>
    /// <returns>HTML Body of an email</returns>
    public static string BuildHtmlEmail(string body, string? footer = null, List<List<string>>? tableData = null, CancellationToken cancellationToken = default)
    {
        tableData ??= [];

        string text = "<html><body>";
        text += body.StringtoHtml();
        text += tableData.Count == 0 ? string.Empty : "<br><br>";
        text += tableData.CreateHtmlTable(cancellationToken: cancellationToken);
        text += !string.IsNullOrWhiteSpace(footer) ? "<br><br>" : string.Empty;
        text += footer.StringtoHtml();
        text += "</body></html>";

        return text.FormatAllUrlsToHtml();
    }

    /// <summary>
    /// Replace line breaks with <br> and tabs with &nbsp&nbsp&nbsp to be HTML compatible
    /// </summary>
    /// <param name="text">String to be formatted</param>
    /// <returns>HTML compatible text</returns>
    public static string StringtoHtml(this string? text)
    {
        if (text != null)
        {
            text = HtmlEncode(text);
            text = text.Replace(Environment.NewLine, "\r").Replace("\n", "\r").Replace("\r", "<br>").Replace("\t", "&nbsp&nbsp&nbsp");
        }
        return text ?? string.Empty;
    }

    /// <summary>
    /// Use regex to find all URLs, ASSUMES URL ENDS WITH WHITESPACE
    /// </summary>
    /// <param name="text">Text to search for and format urls in</param>
    /// <param name="linkText">Text to display for link</param>
    /// <returns>HTML Body with formatted HTML links</returns>
    public static string FormatAllUrlsToHtml(this string text, string? linkText = null)
    {
        Regex regx = UrlRegex();
        MatchCollection matches = regx.Matches(text);
        foreach (Match url in matches.AsEnumerable())
        {
            text = text.Replace(url.Value, url.Value.CreateHtmlLink(linkText ?? "Click Here"));
        }
        return text;
    }

    /// <summary>
    /// Creates a link behind the given text value
    /// </summary>
    /// <param name="url">Url to embed behind he text</param>
    /// <param name="linkText">Text to display for link</param>
    /// <returns>Formatted HTML link</returns>
    public static string CreateHtmlLink(this string url, string linkText = "Click Here")
    {
        return $"<a href=\"{url}\">{linkText}</a>";
    }

    /// <summary>
    /// Create an HTML table from given data
    /// </summary>
    /// <param name="tableData">Data to turn into an HTML table</param>
    /// <param name="applyTableCss">Apply CSS styling to table</param>
    /// <param name="customCss">Custom CSS to apply to the table. If not provided, default will be used.</param>
    /// <returns>HTML table based on the data passed in</returns>
    public static string CreateHtmlTable(this DataTable? tableData, bool applyTableCss = true, string? customCss = null, CancellationToken cancellationToken = default)
    {
        string tableHtml = string.Empty;

        if (tableData != null && tableData.Rows.Count != 0)
        {
            string tableStyle = !string.IsNullOrWhiteSpace(customCss) ? customCss :
                "<style>" +
                    "table{" +
                        "font-family: arial, sans-serif;" +
                        "border-collapse: collapse;" +
                        "width: 100%;" +
                    "}" +
                    "td, th {" +
                        "border: 1px solid #dddddd;" +
                        "text-align: left;" +
                        "padding: 8px;" +
                    "}" +
                    "tr:nth-child(even) {" +
                        "background-color: #dddddd;" +
                    "}" +
                "</style>";

            tableHtml += applyTableCss ? tableStyle : string.Empty;

            //Make headers
            tableHtml += "<table><tr>";
            foreach (DataColumn column in tableData.Columns)
            {
                tableHtml += $"<th>{column.ColumnName}</th>";
            }
            tableHtml += "</tr>";

            //Add data rows
            foreach (DataRow rowData in tableData.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tableHtml += "<tr>";
                tableHtml += string.Concat(rowData.ItemArray.Select(x => $"<td>{x?.ToString()}</td>"));
                tableHtml += "</tr>";
            }
            tableHtml += "</table>";
        }
        return tableHtml;
    }

    /// <summary>
    /// Create an HTML table from given data
    /// </summary>
    /// <param name="tableData">Data to turn into an HTML table. The first item should contain the header values</param>
    /// <param name="applyTableCss">Apply CSS styling to table</param>
    /// <param name="customCss">Custom CSS to apply to the table. If not provided, default will be used.</param>
    /// <returns>HTML table based on the data passed in</returns>
    public static string CreateHtmlTable(this List<List<string>>? tableData, bool applyTableCss = true, string? customCss = null, CancellationToken cancellationToken = default)
    {
        tableData ??= [];
        string tableHtml = string.Empty;
        if (tableData.Count > 0)
        {
            string tableStyle = !string.IsNullOrWhiteSpace(customCss) ? customCss :
                "<style>" +
                    "table{" +
                        "font-family: arial, sans-serif;" +
                        "border-collapse: collapse;" +
                        "width: 100%;" +
                    "}" +
                    "td, th {" +
                        "border: 1px solid #dddddd;" +
                        "text-align: left;" +
                        "padding: 8px;" +
                    "}" +
                    "tr:nth-child(even) {" +
                        "background-color: #dddddd;" +
                    "}" +
                "</style>";

            tableHtml += applyTableCss ? tableStyle : string.Empty;

            List<string> tableHeaders = tableData[0];

            //Make headers
            tableHtml += "<table><tr>";
            tableHtml += string.Concat(tableHeaders.Select(x => $"<th>{x}</th>"));
            tableHtml += "</tr>";

            //Add data rows
            foreach (List<string> rowData in tableData.Skip(1))
            {
                cancellationToken.ThrowIfCancellationRequested();
                tableHtml += "<tr>";
                tableHtml += string.Concat(rowData.Select(x => $"<td>{x}</td>"));
                tableHtml += "</tr>";
            }
            tableHtml += "</table>";
        }
        return tableHtml;
    }
}
