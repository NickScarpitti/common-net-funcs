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
}
