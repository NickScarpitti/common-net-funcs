using CommonNetFuncs.Web.Middleware;

namespace Web.Middleware.Tests;

public sealed class ResponseLoggingConfigTests
{
	[Fact]
	public void Constructor_CreatesInstance()
	{
		// Act
		ResponseLoggingConfig config = new();

		// Assert
		config.ShouldNotBeNull();
	}

	[Fact]
	public void ThresholdInSeconds_CanBeSetAndRetrieved()
	{
		// Arrange
		ResponseLoggingConfig config = new()
		{
			ThresholdInSeconds = 5.5
		};

		// Act & Assert
		config.ThresholdInSeconds.ShouldBe(5.5);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1.5)]
	[InlineData(10.0)]
	[InlineData(-1)]
	public void ThresholdInSeconds_HandlesDifferentValues(double threshold)
	{
		// Arrange
		ResponseLoggingConfig config = new()
		{
			ThresholdInSeconds = threshold
		};

		// Act & Assert
		config.ThresholdInSeconds.ShouldBe(threshold);
	}

	[Fact]
	public void ResponseLoggingConfig_ImplementsIResponseLoggingConfig()
	{
		// Arrange
		ResponseLoggingConfig config = new();

		// Act & Assert
		config.ShouldBeAssignableTo<IResponseLoggingConfig>();
	}
}
