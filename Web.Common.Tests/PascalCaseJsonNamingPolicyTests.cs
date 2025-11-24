using CommonNetFuncs.Web.Common;

namespace Web.Common.Tests;

public sealed class PascalCaseJsonNamingPolicyTests
{
	private readonly PascalCaseJsonNamingPolicy _sut;

	public PascalCaseJsonNamingPolicyTests()
	{
		_sut = new PascalCaseJsonNamingPolicy();
	}

	[Theory]
	[InlineData("firstName", "FirstName")]
	[InlineData("lastName", "LastName")]
	[InlineData("emailAddress", "EmailAddress")]
	[InlineData("user123Name", "User123Name")]
	public void ConvertName_WhenGivenLowerCaseStart_ReturnsPascalCase(string input, string expected)
	{
		// Act
		string result = _sut.ConvertName(input);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("FirstName")]
	[InlineData("LastName")]
	[InlineData("EmailAddress")]
	[InlineData("ID")]
	[InlineData("APIKey")]
	[InlineData("123name")]
	[InlineData("$price")]
	[InlineData("_id")]
	public void ConvertName_WhenGivenPascalCaseOrFirstCharacterNotLetter_ReturnsUnmodified(string input)
	{
		// Act
		string result = _sut.ConvertName(input);

		// Assert
		result.ShouldBe(input);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null)]
	public void ConvertName_WhenGivenNullOrEmpty_ReturnsUnmodified(string? input)
	{
		// Act
		string result = _sut.ConvertName(input!);

		// Assert
		result.ShouldBe(input);
	}

	[Fact]
	public void ConvertName_WhenGivenSingleCharacter_ConvertsToPascalCase()
	{
		// Arrange
		const string input = "a";
		const string expected = "A";

		// Act
		string result = _sut.ConvertName(input);

		// Assert
		result.ShouldBe(expected);
	}
}
