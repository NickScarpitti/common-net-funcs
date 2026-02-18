using CommonNetFuncs.Web.Interface;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Shouldly;

namespace Web.Interface.Tests;

public sealed class DataTableHelpersTests
{
	[Fact]
	public void GetDataTableRequest_InvalidContentType_ReturnsEmptyRequest()
	{
		HttpRequest request = A.Fake<HttpRequest>();
		A.CallTo(() => request.ContentType).Returns("application/json");

		DataTableRequest result = request.GetDataTableRequest();

		result.ShouldNotBeNull();
		result.SearchValues.Count.ShouldBe(0);
		result.PageSize.ShouldBe(0);
		result.Skip.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableRequest_ValidFormData_ParsesCorrectly()
	{
		HttpRequest request = A.Fake<HttpRequest>();

		Dictionary<string, StringValues> formData = new()
			{
				{ "draw", new StringValues("1") },
				{ "start", new StringValues("0") },
				{ "length", new StringValues("10") },
				{ "order[0][column]", new StringValues("1") },
				{ "order[0][dir]", new StringValues("asc") },
				{ "columns[1][data]", new StringValues("name") },
				{ "search[value]", new StringValues("field1=value1,field2=value2") }
			};

		IFormCollection formCollection = new FormCollection(formData);

		A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
		A.CallTo(() => request.Form).Returns(formCollection);

		DataTableRequest result = request.GetDataTableRequest();

		result.Draw.ShouldBe("1");
		result.PageSize.ShouldBe(10);
		result.Skip.ShouldBe(0);
		result.SortColumns[0].ShouldBe("name");
		result.SortColumnDir[0].ShouldBe("asc");
		result.SearchValues["field1"].ShouldBe("value1");
		result.SearchValues["field2"].ShouldBe("value2");
	}

	[Fact]
	public void GetSortAndLimitPostModel_CreatesCorrectModel()
	{
		DataTableRequest request = new()
		{
			PageSize = 10,
			Skip = 0,
			SortColumns = new Dictionary<int, string?> { { 0, "name" } },
			SortColumnDir = new Dictionary<int, string?> { { 0, "asc" } }
		};

		SortAndLimitPostModel result = DataTableHelpers.GetSortAndLimitPostModel(request);

		result.PageSize.ShouldBe(10);
		result.Skip.ShouldBe(0);
		result.SortColumns[0].ShouldBe("name");
		result.SortColumnDir[0].ShouldBe("asc");
	}

	[Fact]
	public void GetDataTableRequest_EmptySearchValue_HandlesCorrectly()
	{
		HttpRequest request = A.Fake<HttpRequest>();
		IFormCollection formCollection = A.Fake<IFormCollection>();

		A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
		A.CallTo(() => request.Form).Returns(formCollection);

		Dictionary<string, StringValues> formData = new()
			{
				{ "search[value]", new StringValues(string.Empty) }
			};

		A.CallTo(() => formCollection.Keys).Returns(formData.Keys);
		foreach (KeyValuePair<string, StringValues> item in formData)
		{
			A.CallTo(() => formCollection[item.Key]).Returns(item.Value);
		}

		DataTableRequest result = request.GetDataTableRequest();

		result.SearchValues.Count.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableRequest_InvalidSearchValueFormat_HandlesGracefully()
	{
		Dictionary<string, StringValues> formData = new()
			{
				{ "search[value]", new StringValues("invalid=") }
			};

		HttpRequest request = A.Fake<HttpRequest>();
		IFormCollection formCollection = new FormCollection(formData);

		A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
		A.CallTo(() => request.Form).Returns(formCollection);

		DataTableRequest result = request.GetDataTableRequest();

		result.SearchValues.Count.ShouldBe(1);
		result.SearchValues["invalid"].ShouldBeNull();
	}

	[Fact]
	public void GetDataTableRequest_ValidContentTypeWithNullForm_ReturnsEmptyRequest()
	{
		HttpRequest request = A.Fake<HttpRequest>();
		A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
		A.CallTo(() => request.Form).Returns(null!);

		DataTableRequest result = request.GetDataTableRequest();

		result.ShouldNotBeNull();
		result.SearchValues.Count.ShouldBe(0);
		result.PageSize.ShouldBe(0);
		result.Skip.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableRequest_SearchValueWithoutEquals_SkipsValue()
	{
		Dictionary<string, StringValues> formData = new()
			{
				{ "search[value]", new StringValues("invalidvalue,field=value") }
			};

		HttpRequest request = A.Fake<HttpRequest>();
		IFormCollection formCollection = new FormCollection(formData);

		A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
		A.CallTo(() => request.Form).Returns(formCollection);

		DataTableRequest result = request.GetDataTableRequest();

		result.SearchValues.Count.ShouldBe(1);
		result.SearchValues.ShouldContainKey("field");
		result.SearchValues["field"].ShouldBe("value");
	}

	[Fact]
	public void GetDataTableRequest_MultipleOrderColumns_ParsesAllCorrectly()
	{
		Dictionary<string, StringValues> formData = new()
			{
				{ "order[0][column]", new StringValues("0") },
				{ "order[0][dir]", new StringValues("asc") },
				{ "order[1][column]", new StringValues("1") },
				{ "order[1][dir]", new StringValues("desc") },
				{ "columns[0][data]", new StringValues("firstName") },
				{ "columns[1][data]", new StringValues("lastName") }
			};

		HttpRequest request = A.Fake<HttpRequest>();
		IFormCollection formCollection = new FormCollection(formData);

		A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
		A.CallTo(() => request.Form).Returns(formCollection);

		DataTableRequest result = request.GetDataTableRequest();

		result.SortColumns.Count.ShouldBe(2);
		result.SortColumns[0].ShouldBe("firstName");
		result.SortColumns[1].ShouldBe("lastName");
		result.SortColumnDir[0].ShouldBe("asc");
		result.SortColumnDir[1].ShouldBe("desc");
	}

	[Fact]
	public void GetDataTableRequest_FormDataContentTypeVariants_ParsesCorrectly()
	{
		Dictionary<string, StringValues> formData = new()
			{
				{ "draw", new StringValues("1") },
				{ "start", new StringValues("5") },
				{ "length", new StringValues("25") }
			};

		HttpRequest request = A.Fake<HttpRequest>();
		IFormCollection formCollection = new FormCollection(formData);

		A.CallTo(() => request.ContentType).Returns("multipart/form-data");
		A.CallTo(() => request.Form).Returns(formCollection);

		DataTableRequest result = request.GetDataTableRequest();

		result.Draw.ShouldBe("1");
		result.PageSize.ShouldBe(25);
		result.Skip.ShouldBe(5);
	}

	[Fact]
	public void GetDataTableRequest_EmptyStartAndLength_HandlesCorrectly()
	{
		Dictionary<string, StringValues> formData = new()
			{
				{ "start", StringValues.Empty },
				{ "length", StringValues.Empty }
			};

		HttpRequest request = A.Fake<HttpRequest>();
		IFormCollection formCollection = new FormCollection(formData);

		A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
		A.CallTo(() => request.Form).Returns(formCollection);

		DataTableRequest result = request.GetDataTableRequest();

		result.PageSize.ShouldBe(0);
		result.Skip.ShouldBe(0);
	}

	[Fact]
	public void DataTableRequest_Constructor_InitializesCollections()
	{
		DataTableRequest request = new();

		request.SearchValues.ShouldNotBeNull();
		request.SortColumnDir.ShouldNotBeNull();
		request.SortColumns.ShouldNotBeNull();
		request.SearchValues.Count.ShouldBe(0);
		request.SortColumnDir.Count.ShouldBe(0);
		request.SortColumns.Count.ShouldBe(0);
	}

	[Fact]
	public void DataTableReturnData_DefaultValues_AreCorrect()
	{
		DataTableReturnData<string> data = new();

		data.Draw.ShouldBeNull();
		data.RecordsFiltered.ShouldBeNull();
		data.RecordsTotal.ShouldBeNull();
		data.Data.ShouldNotBeNull();
		data.Data.ShouldBeEmpty();
	}

	[Fact]
	public void SortAndLimitPostModel_Constructor_InitializesCollections()
	{
		SortAndLimitPostModel model = new();

		model.SortColumns.ShouldNotBeNull();
		model.SortColumnDir.ShouldNotBeNull();
		model.SortColumns.Count.ShouldBe(0);
		model.SortColumnDir.Count.ShouldBe(0);
		model.Skip.ShouldBe(0);
		model.PageSize.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableRequest_ExceptionDuringParsing_ReturnsEmptyRequest()
	{
		HttpRequest request = A.Fake<HttpRequest>();
		A.CallTo(() => request.ContentType).Returns("application/x-www-form-urlencoded");
		A.CallTo(() => request.Form).Throws<InvalidOperationException>();

		DataTableRequest result = request.GetDataTableRequest();

		result.ShouldNotBeNull();
		result.SearchValues.Count.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableRequest_NullContentType_ReturnsEmptyRequest()
	{
		HttpRequest request = A.Fake<HttpRequest>();
		A.CallTo(() => request.ContentType).Returns(null);
		A.CallTo(() => request.Form).Returns(new FormCollection(new Dictionary<string, StringValues>()));

		DataTableRequest result = request.GetDataTableRequest();

		result.ShouldNotBeNull();
		result.SearchValues.Count.ShouldBe(0);
	}
}
