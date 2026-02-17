using System.ComponentModel.DataAnnotations;
using CommonNetFuncs.Web.Common.ValidationAttributes;
using static CommonNetFuncs.Web.Common.ValidationAttributes.ListMaxLengthAttribute;

namespace Web.Common.Tests.ValidationAttributes;

// Custom collection that is NOT ICollection but has a Count property (for reflection test)
public class CustomCollectionWithCount : IEnumerable<string?>
{
	private readonly List<string?> _items;

	public CustomCollectionWithCount(List<string?> items)
	{
		_items = items;
	}

	public int Count => _items.Count;

	public IEnumerator<string?> GetEnumerator() => _items.GetEnumerator();
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// Custom collection with NO Count property (for InvalidCastException test)
public class CustomCollectionNoCount : IEnumerable<string?>
{
	private readonly List<string?> _items;

	public CustomCollectionNoCount(List<string?> items)
	{
		_items = items;
	}

	public IEnumerator<string?> GetEnumerator() => _items.GetEnumerator();
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// Custom collection with write-only Count property - commented out due to compilation warnings
// public class CustomCollectionWriteOnlyCount
// {
// 	public int Count { set { } }
// }

public sealed class ListMaxLengthAttributeTests : ValidationTestBase
{
	[Fact]
	public void Constructor_WithValidLength_ShouldSetLengthProperty()
	{
		// Arrange & Act
		ListMaxLengthAttribute attribute = new(10);

		// Assert
		attribute.Length.ShouldBe(10);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-2)]
	public void Constructor_WithInvalidLength_ShouldThrowOnValidation(int length)
	{
		// Arrange
		ListMaxLengthAttribute attribute = new(length);
		List<string> list = new() { "test" };

		// Act & Assert
		Should.Throw<InvalidOperationException>(() => attribute.GetValidationResult(list, DummyValidationContext));
	}

	[Fact]
	public void IsValid_WithNull_ShouldReturnSuccess()
	{
		// Arrange
		ListMaxLengthAttribute attribute = new(5);

		// Act
		ValidationResult? result = attribute.GetValidationResult(null, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithNonCollection_ShouldThrow()
	{
		// Arrange
		ListMaxLengthAttribute attribute = new(5);
		const string nonCollection = "not a collection";

		// Act & Assert
		Should.Throw<InvalidDataException>(() => attribute.GetValidationResult(nonCollection, DummyValidationContext));
	}

	[Theory]
	[InlineData(5, new[] { "1234", "123", "12345" }, true)]
	[InlineData(3, new[] { "1234", "12345" }, false)]
	public void IsValid_WithStringList_ShouldValidateCorrectly(int maxLength, string[] items, bool shouldBeValid)
	{
		// Arrange
		ListMaxLengthAttribute attribute = new(maxLength);
		List<string> list = new(items);

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, CreateValidationContext("TestProperty"));

		// Assert
		if (shouldBeValid)
		{
			result.ShouldBe(ValidationResult.Success);
		}
		else
		{
			result.ShouldNotBeNull();
			result.ErrorMessage?.ShouldContain("exceeds the maximum length");
		}
	}

	[Fact]
	public void FormatErrorMessage_ShouldReturnFormattedMessage()
	{
		// Arrange
		ListMaxLengthAttribute attribute = new(5);

		// Act
		string message = attribute.FormatErrorMessage("TestProperty");

		// Assert
		message.ShouldContain("TestProperty");
		message.ShouldContain("5");
	}

	[Fact]
	public void Constructor_Parameterless_ShouldSetLengthToMaxAllowable()
	{
		// Arrange & Act
		ListMaxLengthAttribute attribute = new();

		// Assert
		attribute.Length.ShouldBe(-1); // MaxAllowableLength constant
	}

	[Fact]
	public void Constructor_WithValidLength_ShouldHaveDefaultErrorMessage()
	{
		// Arrange & Act
		ListMaxLengthAttribute attribute = new(5);

		// Trigger the error message lambda from base constructor
		_ = attribute.ErrorMessageResourceType;
		string errorMessage = attribute.FormatErrorMessage("TestField");

		// Assert
		errorMessage.ShouldContain("TestField");
		errorMessage.ShouldContain("5");
		errorMessage.ShouldContain("cannot be longer than");
	}

	[Fact]
	public void Parameterless_Constructor_ShouldHaveDefaultErrorMessage()
	{
		// Arrange & Act
		ListMaxLengthAttribute attribute = new();

		// Trigger the error message lambda  from base constructor
		_ = attribute.ErrorMessageResourceType;
		string errorMessage = attribute.FormatErrorMessage("TestField");

		// Assert
		errorMessage.ShouldContain("TestField");
		errorMessage.ShouldContain("-1");
		errorMessage.ShouldContain("cannot be longer than");
	}

	[Fact]
	public void IsValid_WithParameterlessConstructor_ShouldAlwaysPassForAnyLength()
	{
		// Arrange
		ListMaxLengthAttribute attribute = new(); // No max length specified (Length = -1)
		List<string> list = ["a", "very long string that would normally fail", "x"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert - should pass because MaxAllowableLength == Length check on line 126
		result.ShouldBe(ValidationResult.Success);
	}

	[Theory]
	[InlineData(5)]
	[InlineData(10)]
	public void Constructor_WithSpecificLength_ShouldValidateItemLength(int length)
	{
		// Arrange
		ListMaxLengthAttribute attribute = new(length);
		List<string> list = ["short"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void IsValid_WithNullItemInCollection_ShouldContinue()
	{
		// Arrange
		ListMaxLengthAttribute attribute = new(5);
		List<string?> list = ["test", null, "abc"];

		// Act
		ValidationResult? result = attribute.GetValidationResult(list, DummyValidationContext);

		// Assert
		result.ShouldBe(ValidationResult.Success);
	}

	[Fact]
	public void CountPropertyHelper_TryGetCount_WithICollection_ShouldReturnTrueAndCount()
	{
		// Arrange
		List<string> collection = ["a", "b", "c"];

		// Act
		bool result = ListMaxLengthAttribute.CountPropertyHelper.TryGetCount(collection, out int count);

		// Assert
		result.ShouldBeTrue();
		count.ShouldBe(3);
	}

	[Fact]
	public void CountPropertyHelper_TryGetCount_WithCustomObjectHavingCountProperty_ShouldUseReflection()
	{
		// Arrange - object with Count property but NOT ICollection
		CustomCollectionWithCount customObject = new(["a", "b"]);

		// Act
		bool result = ListMaxLengthAttribute.CountPropertyHelper.TryGetCount(customObject, out int count);

		// Assert
		result.ShouldBeTrue();
		count.ShouldBe(2);
	}

	[Fact]
	public void CountPropertyHelper_TryGetCount_WithObjectNoCountProperty_ShouldReturnFalse()
	{
		// Arrange - object without Count property
		CustomCollectionNoCount customObject = new(["x"]);

		// Act
		bool result = ListMaxLengthAttribute.CountPropertyHelper.TryGetCount(customObject, out int count);

		// Assert
		result.ShouldBeFalse();
		count.ShouldBe(-1);
	}

	[Fact]
	public void CountPropertyHelper_TryGetCount_WithObjectHavingNonIntCountProperty_ShouldReturnFalse()
	{
		// Arrange - object with Count property of wrong type
		var customObject = new { Count = "three" };

		// Act
		bool result = ListMaxLengthAttribute.CountPropertyHelper.TryGetCount(customObject, out int count);

		// Assert
		result.ShouldBeFalse();
		count.ShouldBe(-1);
	}

	[Fact]
	public void CountPropertyHelper_TryGetCount_WithObjectHavingNullCountProperty_ShouldReturnFalse()
	{
		// Arrange - object without any Count property
		var customObject = new { Name = "test" };

		// Act
		bool result = ListMaxLengthAttribute.CountPropertyHelper.TryGetCount(customObject, out int count);

		// Assert
		result.ShouldBeFalse();
		count.ShouldBe(-1);
	}
}
