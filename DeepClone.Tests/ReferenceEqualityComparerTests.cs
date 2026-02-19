namespace DeepClone.Tests;

public enum ReferenceEqualityScenario
{
	BothNull,
	SameReference,
	DifferentReferences
}

public sealed class ReferenceEqualityComparerTests
{
	private readonly Fixture fixture;
	private readonly CommonNetFuncs.DeepClone.ReferenceEqualityComparer comparer;

	public ReferenceEqualityComparerTests()
	{
		fixture = new Fixture();
		comparer = new CommonNetFuncs.DeepClone.ReferenceEqualityComparer();
	}

	[Theory]
	[InlineData(ReferenceEqualityScenario.BothNull, true)]
	[InlineData(ReferenceEqualityScenario.SameReference, true)]
	[InlineData(ReferenceEqualityScenario.DifferentReferences, false)]
	public void Equals_WithVariousScenarios_ReturnsExpectedResult(ReferenceEqualityScenario scenario, bool expected)
	{
		// Arrange & Act
		bool result;
		switch (scenario)
		{
			case ReferenceEqualityScenario.BothNull:
				result = comparer.Equals(null, null);
				break;

			case ReferenceEqualityScenario.SameReference:
				object obj = new();
				result = comparer.Equals(obj, obj);
				break;

			case ReferenceEqualityScenario.DifferentReferences:
				string str1 = fixture.Create<string>();
				string str2 = new(str1.ToCharArray());
				result = comparer.Equals(str1, str2);
				break;

			default:
				throw new ArgumentException("Invalid scenario");
		}

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(true, 0)]  // null object
	[InlineData(false, 0)] // non-null (0 is placeholder, will be replaced with actual hash)
	public void GetHashCode_WithNullAndNonNull_ReturnsExpectedValue(bool isNull, int placeholderExpected)
	{
		// Arrange
		object? obj = isNull ? null : new object();
		int expected = isNull ? 0 : obj!.GetHashCode();

		// Act
		int result = comparer.GetHashCode(obj!);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void GetHashCode_WhenCalledMultipleTimes_ShouldReturnSameValue()
	{
		// Arrange
		object obj = new();

		// Act
		int firstCall = comparer.GetHashCode(obj);
		int secondCall = comparer.GetHashCode(obj);

		// Assert
		firstCall.ShouldBe(secondCall);
	}
}
