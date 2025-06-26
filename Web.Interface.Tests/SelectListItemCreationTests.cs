using AutoFixture;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shouldly;
using CommonNetFuncs.Web.Interface;

namespace Web.Interface.Tests;

public sealed class SelectListItemCreationTests
{
    private readonly Fixture _fixture;

    public SelectListItemCreationTests()
    {
        _fixture = new Fixture();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    public void ToSelectListItem_NullString_ReturnsNull(string? value, bool selected)
    {
        value.ToSelectListItem(selected).ShouldBeNull();
    }

    [Theory]
    [InlineData("test", true)]
    [InlineData("test", false)]
    public void ToSelectListItem_ValidString_ReturnsCorrectItem(string value, bool selected)
    {
        SelectListItem result = value.ToSelectListItem(selected);

        result.Value.ShouldBe(value);
        result.Text.ShouldBe(value);
        result.Selected.ShouldBe(selected);
    }

    [Theory]
    [InlineData(null)]
    public void ToSelectListItem_NullStringWithoutSelected_ReturnsNull(string? value)
    {
        value.ToSelectListItem().ShouldBeNull();
    }

    [Fact]
    public void ToSelectListItem_ValidStringWithoutSelected_ReturnsCorrectItem()
    {
        string value = _fixture.Create<string>();

        SelectListItem result = value.ToSelectListItem();

        result.Value.ShouldBe(value);
        result.Text.ShouldBe(value);
        result.Selected.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null, "text", true)]
    [InlineData(null, null, false)]
    public void ToSelectListItem_NullStringWithText_ReturnsNull(string? value, string? text, bool selected)
    {
        value.ToSelectListItem(text, selected).ShouldBeNull();
    }

    [Theory]
    [InlineData("value", null, true)]
    [InlineData("value", "text", false)]
    public void ToSelectListItem_ValidStringWithText_ReturnsCorrectItem(string value, string? text, bool selected)
    {
        SelectListItem result = value.ToSelectListItem(text, selected);

        result.Value.ShouldBe(value);
        result.Text.ShouldBe(text ?? string.Empty);
        result.Selected.ShouldBe(selected);
    }

    [Theory]
    [InlineData(null, null)]
    public void ToSelectListItem_NullStringWithTextWithoutSelected_ReturnsNull(string? value, string? text)
    {
        value.ToSelectListItem(text).ShouldBeNull();
    }

    [Theory]
    [InlineData("value", null)]
    [InlineData("value", "text")]
    public void ToSelectListItem_ValidStringWithTextWithoutSelected_ReturnsCorrectItem(string value, string? text)
    {
        SelectListItem result = value.ToSelectListItem(text);

        result.Value.ShouldBe(value);
        result.Text.ShouldBe(text ?? string.Empty);
        result.Selected.ShouldBeFalse();
    }

    // Integer Tests
    [Theory]
    [InlineData(null, true)]
    [InlineData(null, false)]
    public void ToSelectListItem_NullInt_ReturnsNull(int? value, bool selected)
    {
        value.ToSelectListItem(selected).ShouldBeNull();
    }

    [Theory]
    [InlineData(42, true)]
    [InlineData(42, false)]
    public void ToSelectListItem_ValidInt_ReturnsCorrectItem(int value, bool selected)
    {
        SelectListItem result = value.ToSelectListItem(selected);

        result.Value.ShouldBe(value.ToString());
        result.Text.ShouldBe(value.ToString());
        result.Selected.ShouldBe(selected);
    }

    [Fact]
    public void ToSelectListItem_ValidIntWithoutSelected_ReturnsCorrectItem()
    {
        int value = _fixture.Create<int>();

        SelectListItem result = value.ToSelectListItem();

        result.Value.ShouldBe(value.ToString());
        result.Text.ShouldBe(value.ToString());
        result.Selected.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null, "text")]
    public void ToSelectListItem_NullIntWithText_ReturnsNull(int? value, string? text)
    {
        value.ToSelectListItem(text).ShouldBeNull();
    }

    [Theory]
    [InlineData(42, null)]
    [InlineData(42, "text")]
    public void ToSelectListItem_ValidIntWithText_ReturnsCorrectItem(int value, string? text)
    {
        SelectListItem result = value.ToSelectListItem(text);

        result.Value.ShouldBe(value.ToString());
        result.Text.ShouldBe(text ?? string.Empty);
        result.Selected.ShouldBeFalse();
    }

    [Theory]
    [InlineData(42, null, true)]
    [InlineData(42, "text", false)]
    public void ToSelectListItem_ValidIntWithTextAndSelected_ReturnsCorrectItem(int value, string? text, bool selected)
    {
        SelectListItem result = value.ToSelectListItem(text, selected);

        result.Value.ShouldBe(value.ToString());
        result.Text.ShouldBe(text ?? string.Empty);
        result.Selected.ShouldBe(selected);
    }
}
