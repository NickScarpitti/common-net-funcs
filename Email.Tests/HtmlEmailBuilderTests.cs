using System.Data;
using CommonNetFuncs.Email;
using static CommonNetFuncs.Email.HtmlEmailBuilder;

namespace Email.Tests;

public sealed class HtmlEmailBuilderTests
{
	[Theory]
	[InlineData("Hello\nWorld", "Hello<br>World")]
	[InlineData("Hello\r\nWorld", "Hello<br>World", "Hello<br><br>World")]
	[InlineData("Hello\tWorld", "Hello&nbsp&nbsp&nbspWorld")]
	[InlineData(null, "")]
	[InlineData("Hello <script>", "Hello &lt;script&gt;")]
	public void StringToHtml_ShouldFormatCorrectly(string? input, string expected, string? unixExpected = null)
	{
		// Act
		string result = input.StringtoHtml();

		// Assert
		result.ShouldBe(Environment.OSVersion.Platform == PlatformID.Win32NT ? expected : unixExpected ?? expected);
	}

	[Theory]
	[InlineData("Visit http://example.com today!", "<a href=\"http://example.com\">Click Here</a>")]
	[InlineData("Check https://example.com/path?q=1 now", "<a href=\"https://example.com/path?q=1\">Click Here</a>")]
	[InlineData("No URL here", "No URL here")]
	public void FormatAllUrlsToHtml_ShouldReplaceUrlsWithLinks(string input, string expectedUrl)
	{
		// Act
		string result = input.FormatAllUrlsToHtml();

		// Assert
		result.ShouldContain(expectedUrl);
	}

	[Theory]
	[InlineData("Custom Text")]
	public void FormatAllUrlsToHtml_WithCustomLinkText_ShouldUseProvidedText(string linkText)
	{
		// Arrange
		const string input = "Visit http://example.com today!";
		string expected = $"<a href=\"http://example.com\">{linkText}</a>";

		// Act
		string result = input.FormatAllUrlsToHtml(linkText);

		// Assert
		result.ShouldContain(expected);
	}

	[Theory]
	[InlineData("http://example.com", "Link Text", "<a href=\"http://example.com\">Link Text</a>")]
	public void CreateHtmlLink_ShouldCreateValidHtmlLink(string url, string linkText, string expected)
	{
		// Act
		string result = url.CreateHtmlLink(linkText);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void BuildHtmlEmail_WithBasicContent_ShouldCreateValidHtml()
	{
		// Arrange
		const string body = "Hello World";
		const string footer = "Goodbye";

		// Act
		string result = BuildHtmlEmail(body, footer);

		// Assert
		result.ShouldStartWith("<html><body>");
		result.ShouldEndWith("</body></html>");
		result.ShouldContain("Hello World");
		result.ShouldContain("Goodbye");
	}

	[Fact]
	public void CreateHtmlTable_WithDataTable_ShouldCreateValidHtmlTable()
	{
		// Arrange
		using DataTable table = new();
		table.Columns.Add("Name");
		table.Columns.Add("Age");
		table.Rows.Add("John", "30");
		table.Rows.Add("Jane", "25");

		// Act
		string result = table.CreateHtmlTable();

		// Assert
		result.ShouldContain("<table>");
		result.ShouldEndWith("</table>");
		result.ShouldContain("<th>Name</th>");
		result.ShouldContain("<th>Age</th>");
		result.ShouldContain("<td>John</td>");
		result.ShouldContain("<td>30</td>");
		result.ShouldContain("<td>Jane</td>");
		result.ShouldContain("<td>25</td>");
	}

	[Fact]
	public void CreateHtmlEmail_WithDataTable_ShouldCreateValidHtmlEmailWithTable()
	{
		// Arrange
		const string body = "Hello";
		const string footer = "Goodbye";
		using DataTable table = new();
		table.Columns.Add("Name");
		table.Columns.Add("Age");
		table.Rows.Add("John", "30");
		table.Rows.Add("Jane", "25");

		// Act
		string result = BuildHtmlEmail(body, table, footer);

		// Assert
		result.ShouldContain("<table>");
		result.ShouldContain("</table>");
		result.ShouldContain("<th>Name</th>");
		result.ShouldContain("<th>Age</th>");
		result.ShouldContain("<td>John</td>");
		result.ShouldContain("<td>30</td>");
		result.ShouldContain("<td>Jane</td>");
		result.ShouldContain("<td>25</td>");
		result.ShouldContain("<br><br>");
		result.ShouldContain(body);
		result.ShouldContain(footer);
	}

	[Fact]
	public void CreateHtmlTable_WithList_ShouldCreateValidHtmlTable()
	{
		// Arrange
		List<List<string>> tableData =
		[
				new() { "Name", "Age" },          // Header row
            new() { "John", "30" },           // Data row 1
            new() { "Jane", "25" }            // Data row 2
		];

		// Act
		string result = tableData.CreateHtmlTable();

		// Assert
		result.ShouldContain("<table>");
		result.ShouldContain("<th>Name</th>");
		result.ShouldContain("<th>Age</th>");
		result.ShouldContain("<td>John</td>");
		result.ShouldContain("<td>30</td>");
		result.ShouldContain("<td>Jane</td>");
		result.ShouldContain("<td>25</td>");
	}

	[Fact]
	public void CreateHtmlTable_WithCustomCss_ShouldApplyCustomStyles()
	{
		// Arrange
		List<List<string>> tableData =
		[
				new() { "Header" },
						new() { "Data" }
		];
		const string customCss = "<style>table { color: red; }</style>";

		// Act
		string result = tableData.CreateHtmlTable(customCss: customCss);

		// Assert
		result.ShouldContain(customCss);
	}

	[Fact]
	public void CreateHtmlTable_WithoutCss_ShouldNotIncludeStyles()
	{
		// Arrange
		List<List<string>> tableData = new()
				{
						new() { "Header" },
						new() { "Data" }
				};

		// Act
		string result = tableData.CreateHtmlTable(applyTableCss: false);

		// Assert
		result.ShouldNotContain("<style>");
	}

	[Fact]
	public void BuildHtmlEmail_WithTableData_ShouldIncludeTableAndSpacing()
	{
		// Arrange
		const string body = "Hello";
		const string footer = "Goodbye";
		List<List<string>> tableData = new()
				{
						new() { "Header" },
						new() { "Data" }
				};

		// Act
		string result = BuildHtmlEmail(body, tableData, footer);

		// Assert
		result.ShouldContain("<br><br>");
		result.ShouldContain("<table>");
		result.ShouldContain(body);
		result.ShouldContain(footer);
	}

	[Fact]
	public void CreateHtmlTable_WithEmptyData_ShouldReturnEmptyString()
	{
		// Arrange
		using DataTable? emptyTable = null;
		List<List<string>>? emptyList = null;

		// Act
		string resultFromTable = emptyTable.CreateHtmlTable();
		string resultFromList = emptyList.CreateHtmlTable();

		// Assert
		resultFromTable.ShouldBe(string.Empty);
		resultFromList.ShouldBe(string.Empty);
	}

	[Fact]
	public async Task CreateHtmlTable_WithCancellation_ShouldRespectCancellationToken()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		List<List<string>> tableData = new()
				{
						new() { "Header" },
						new() { "Data1" },
						new() { "Data2" }
				};
		await cts.CancelAsync();

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(() =>
		{
			_ = tableData.CreateHtmlTable(cancellationToken: cts.Token);
			return Task.CompletedTask;
		});
	}

	[Theory]
	[InlineData(true, "<br><br>")]
	[InlineData(false, "")]
	public void BuildHtmlEmail_WithOptionalFooter_ShouldHandleSpacingCorrectly(bool includeFooter, string expectedSpacing)
	{
		// Arrange
		const string body = "Hello";
		string? footer = includeFooter ? "Footer" : null;

		// Act
		string result = BuildHtmlEmail(body, footer);

		// Assert
		if (includeFooter)
		{
			result.ShouldContain(expectedSpacing);
		}
		else
		{
			result.ShouldNotContain("<br><br>");
		}
	}
}