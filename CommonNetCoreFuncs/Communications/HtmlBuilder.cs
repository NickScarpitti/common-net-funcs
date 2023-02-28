using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace CommonNetCoreFuncs.Communications;

static public class HtmlBuilder
{
    static public string BuildHtmlString(string body, List<string>? tableHeaders = null, List<List<string>>? tableData = null)
    {
        string text = "<html><body>";

        text += StringtoHtml(body);
        text += CreateHtmlTable(tableHeaders, tableData);
        FormatAllUrlsToHtml(ref text);

        text += "</body></html>";
        return text;
    }

    static private string StringtoHtml(string text)
    {
        //replace linebreaks
        text = HttpUtility.HtmlEncode(text);
        text = text.Replace("\r\n", "\r");
        text = text.Replace("\n", "\r");
        text = text.Replace("\r", "<br>");


        //replace tabs
        text = text.Replace("\t", "&nbsp&nbsp&nbsp");

        return text;

    }


    static private void FormatAllUrlsToHtml(ref string text)
    {
        // use regexpression to find all of URLs, ASSUMES URL ENDS WITH WHITESPACE
        Regex regx = new Regex(@"https?://\S+", RegexOptions.IgnoreCase);
        MatchCollection matches = regx.Matches(text);
        foreach (Match url in matches)
        {
            text = text.Replace(url.Value, CreateHtmlLink(url.Value));
        }
    }

    static private string CreateHtmlLink(string url, string linkText = "Click Here")
    {
        string text = $"<a href=\"{url}\">{linkText}</a>";
        return text;
    }

    static private string CreateHtmlTable(List<string>? tableHeaders, List<List<string>>? tableData, bool applyFancyStyle = true)
    {
        string text = string.Empty;
        string tableStyle =
            "<style> " +
                "table{" +
                    "font-family: arial, sans-serif;" +
                    "border-collapse: collapse;" +
                    "width: 100 %;}" +
                "td, th {" +
                    "border: 1px solid #dddddd;" +
                    "text-align: left;" +
                    "padding: 8px;} " +
                "tr: nth-child(even) { " +
                    "background-color: #dddddd;}" +
            "</style> ";

        if (tableHeaders == null || tableHeaders.Count <= 0 || tableData == null || tableData.Count <= 0)
        {
            return text;
        }

        //make headers
        text = "<table><tr>";
        foreach (var hdr in tableHeaders)
        {
            text += "<th>" + hdr + "</th>";
        }
        text += "</tr>";

        //add data rows
        foreach (var row in tableData)
        {
            text += "<tr>";
            foreach (var dataItem in row)
            {
                text += "<td>" + dataItem + "</td>";
            }
            text += "</tr>";
        }

        text += "</table>";

        if (applyFancyStyle)
        {
            text = tableStyle + text;
        }
        return text;
    }
}
