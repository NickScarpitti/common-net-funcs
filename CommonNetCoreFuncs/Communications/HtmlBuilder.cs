using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace CommonNetCoreFuncs.Communications;

public static class HtmlBuilder
{
    public static string BuildHtmlString(string body, string footer, List<List<string>>? tableData = null)
    {
        string text = "<html><body>";

        text += StringtoHtml(body);
        text += CreateHtmlTable(tableData);
        text += StringtoHtml(footer);
        FormatAllUrlsToHtml(ref text);

        text += "</body></html>";
        return text;
    }

    public static string StringtoHtml(string text)
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


    private static void FormatAllUrlsToHtml(ref string text)
    {
        // use regexpression to find all of URLs, ASSUMES URL ENDS WITH WHITESPACE
        Regex regx = new Regex(@"https?://[^\n\t< ]+", RegexOptions.IgnoreCase);
        MatchCollection matches = regx.Matches(text);
        foreach (Match url in matches)
        {
            text = text.Replace(url.Value, CreateHtmlLink(url.Value));
        }
    }

    private static string CreateHtmlLink(string url, string linkText = "Click Here")
    {
        string text = $"<a href=\"{url}\">{linkText}</a>";
        return text;
    }

    private static string CreateHtmlTable(List<List<string>>? tableData, bool applyFancyStyle = true)
    {
        string text = "<br><br>";
        string tableStyle =
            "<style>table{font-family: arial, sans-serif;border-collapse: collapse;width: 100%;}td, th {border: 1px solid #dddddd;text-align: left;padding: 8px;}tr:nth-child(even) {background - color: #dddddd;}</style>";

        if (tableData == null || tableData.Count <= 0)
        {
            return text;
        }

        if (applyFancyStyle)
        {
            text += tableStyle;
        }

        List<string> tableHeaders = tableData[0];

        //make headers
        text += "<table><tr>";
        foreach (var hdr in tableHeaders)
        {
            text += "<th>" + hdr + "</th>";
        }
        text += "</tr>";

        //add data rows
        foreach (var row in tableData)
        {
            if (row == tableHeaders)
            {
                continue;
            }
            text += "<tr>";
            foreach (var dataItem in row)
            {
                text += "<td>" + dataItem + "</td>";
            }
            text += "</tr>";
        }

        text += "</table>";

        
        return text;
    }
}
