using CommonNetFuncs.Hangfire;

namespace Hangfire.Tests;

public enum ThrowMethod
{
	EntityNotFound,
	ValidationFailed,
	DependencyUnavailable
}

public enum MessageComponent
{
	OperationName,
	EntityId,
	PermanentFailureTag,
	NoPermanentFailureTag
}

public sealed class HangfireJobExceptionTests
{
	private readonly Fixture fixture;

	public HangfireJobExceptionTests()
	{
		fixture = new Fixture();
	}

	[Fact]
	public void Constructor_Default_ShouldInitializeWithDefaults()
	{
		// Act
		HangfireJobException exception = new();

		// Assert
		Assert.NotNull(exception);
		Assert.True(exception.AllowRetry);
		Assert.Null(exception.OperationName);
		Assert.Null(exception.EntityId);
		Assert.NotNull(exception.Message);
		Assert.NotEmpty(exception.Message);
	}

	[Fact]
	public void Constructor_WithMessage_ShouldSetMessage()
	{
		// Arrange
		string message = fixture.Create<string>();

		// Act
		HangfireJobException exception = new(message);

		// Assert
		Assert.Contains(message, exception.Message);
		Assert.True(exception.AllowRetry);
		Assert.Null(exception.OperationName);
		Assert.Null(exception.EntityId);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Constructor_WithMessageAndAllowRetry_ShouldSetProperties(bool allowRetry)
	{
		// Arrange
		string message = fixture.Create<string>();

		// Act
		HangfireJobException exception = new(message, allowRetry);

		// Assert
		Assert.Contains(message, exception.Message);
		Assert.Equal(allowRetry, exception.AllowRetry);
		Assert.Null(exception.OperationName);
		Assert.Null(exception.EntityId);
	}

	[Fact]
	public void Constructor_WithMessageAndInnerException_ShouldSetProperties()
	{
		// Arrange
		string message = fixture.Create<string>();
		Exception innerException = new InvalidOperationException("Inner exception");

		// Act
		HangfireJobException exception = new(message, innerException);

		// Assert
		Assert.Contains(message, exception.Message);
		Assert.Equal(innerException, exception.InnerException);
		Assert.True(exception.AllowRetry);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Constructor_WithMessageInnerExceptionAndAllowRetry_ShouldSetProperties(bool allowRetry)
	{
		// Arrange
		string message = fixture.Create<string>();
		Exception innerException = new InvalidOperationException("Inner exception");

		// Act
		HangfireJobException exception = new(message, innerException, allowRetry);

		// Assert
		Assert.Contains(message, exception.Message);
		Assert.Equal(innerException, exception.InnerException);
		Assert.Equal(allowRetry, exception.AllowRetry);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Constructor_WithMessageOperationAndAllowRetry_ShouldSetProperties(bool allowRetry)
	{
		// Arrange
		string message = fixture.Create<string>();
		string operationName = fixture.Create<string>();

		// Act
		HangfireJobException exception = new(message, operationName, allowRetry);

		// Assert
		Assert.Contains(message, exception.Message);
		Assert.Contains(operationName, exception.Message);
		Assert.Equal(operationName, exception.OperationName);
		Assert.Equal(allowRetry, exception.AllowRetry);
		Assert.Null(exception.EntityId);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Constructor_WithFullContext_ShouldSetAllProperties(bool allowRetry)
	{
		// Arrange
		string message = fixture.Create<string>();
		string operationName = fixture.Create<string>();
		int entityId = fixture.Create<int>();

		// Act
		HangfireJobException exception = new(message, operationName, entityId, allowRetry);

		// Assert
		Assert.Contains(message, exception.Message);
		Assert.Contains(operationName, exception.Message);
		Assert.Contains(entityId.ToString(), exception.Message);
		Assert.Equal(operationName, exception.OperationName);
		Assert.Equal(entityId, exception.EntityId);
		Assert.Equal(allowRetry, exception.AllowRetry);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Constructor_WithFullContextAndInnerException_ShouldSetAllProperties(bool allowRetry)
	{
		// Arrange
		string message = fixture.Create<string>();
		string operationName = fixture.Create<string>();
		int entityId = fixture.Create<int>();
		Exception innerException = new InvalidOperationException("Inner exception");

		// Act
		HangfireJobException exception = new(message, operationName, entityId, innerException, allowRetry);

		// Assert
		Assert.Contains(message, exception.Message);
		Assert.Contains(operationName, exception.Message);
		Assert.Contains(entityId.ToString(), exception.Message);
		Assert.Equal(operationName, exception.OperationName);
		Assert.Equal(entityId, exception.EntityId);
		Assert.Equal(innerException, exception.InnerException);
		Assert.Equal(allowRetry, exception.AllowRetry);
	}

	[Theory]
	[InlineData(MessageComponent.OperationName)]
	[InlineData(MessageComponent.EntityId)]
	[InlineData(MessageComponent.PermanentFailureTag)]
	[InlineData(MessageComponent.NoPermanentFailureTag)]
	public void Message_ShouldIncludeComponent_WhenSet(MessageComponent component)
	{
		// Arrange & Act
		HangfireJobException exception = component switch
		{
			MessageComponent.OperationName =>
				new HangfireJobException("Test error", "TestOperation"),
			MessageComponent.EntityId =>
				new HangfireJobException("Test error", "TestOperation", 123),
			MessageComponent.PermanentFailureTag =>
				new HangfireJobException("Test error", allowRetry: false),
			MessageComponent.NoPermanentFailureTag =>
				new HangfireJobException("Test error", allowRetry: true),
			_ => throw new ArgumentException("Invalid component")
		};

		// Assert
		switch (component)
		{
			case MessageComponent.OperationName:
				Assert.Contains("[Operation: TestOperation]", exception.Message);
				Assert.Contains("Test error", exception.Message);
				break;
			case MessageComponent.EntityId:
				Assert.Contains("(Entity ID: 123)", exception.Message);
				break;
			case MessageComponent.PermanentFailureTag:
				Assert.Contains("[PERMANENT FAILURE - NO RETRY]", exception.Message);
				break;
			case MessageComponent.NoPermanentFailureTag:
				Assert.DoesNotContain("[PERMANENT FAILURE - NO RETRY]", exception.Message);
				break;
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ThrowEntityNotFound_WithIntId_ShouldThrowCorrectException(bool allowRetry)
	{
		// Arrange
		const string entityType = "User";
		const int entityId = 123;
		const string operationName = "UpdateUser";

		// Act & Assert
		HangfireJobException exception = Assert.Throws<HangfireJobException>(() => HangfireJobException.ThrowEntityNotFound(entityType, entityId, operationName, allowRetry));

		Assert.Contains("User with ID 123 not found", exception.Message);
		Assert.Equal(operationName, exception.OperationName);
		Assert.Equal(entityId, exception.EntityId);
		Assert.Equal(allowRetry, exception.AllowRetry);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ThrowEntityNotFound_WithStringId_ShouldThrowCorrectException(bool allowRetry)
	{
		// Arrange
		const string entityType = "Document";
		const string entityId = "doc-12345";
		const string operationName = "ProcessDocument";

		// Act & Assert
		HangfireJobException exception = Assert.Throws<HangfireJobException>(() => HangfireJobException.ThrowEntityNotFound(entityType, entityId, operationName, allowRetry));

		Assert.Contains("Document with ID 'doc-12345' not found", exception.Message);
		Assert.Equal(operationName, exception.OperationName);
		Assert.Equal(allowRetry, exception.AllowRetry);
		Assert.Null(exception.EntityId); // String IDs don't set EntityId property
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ThrowEntityNotFound_WithLongId_ShouldThrowCorrectException(bool allowRetry)
	{
		// Arrange
		const string entityType = "Transaction";
		const long entityId = 9876543210L;
		const string operationName = "ProcessTransaction";

		// Act & Assert
		HangfireJobException exception = Assert.Throws<HangfireJobException>(() => HangfireJobException.ThrowEntityNotFound(entityType, entityId, operationName, allowRetry));

		Assert.Contains("Transaction with ID 9876543210 not found", exception.Message);
		Assert.Equal(operationName, exception.OperationName);
		Assert.Equal(allowRetry, exception.AllowRetry);
		Assert.Null(exception.EntityId); // Long IDs don't set EntityId property (which is int?)
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ThrowValidationFailed_ShouldThrowCorrectException(bool allowRetry)
	{
		// Arrange
		const string validationMessage = "Email format is invalid";
		const string operationName = "ValidateUser";
		const int entityId = 456;

		// Act & Assert
		HangfireJobException exception = Assert.Throws<HangfireJobException>(() => HangfireJobException.ThrowValidationFailed(validationMessage, operationName, entityId, allowRetry));

		Assert.Contains("Validation failed: Email format is invalid", exception.Message);
		Assert.Equal(operationName, exception.OperationName);
		Assert.Equal(entityId, exception.EntityId);
		Assert.Equal(allowRetry, exception.AllowRetry);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ThrowDependencyUnavailable_ShouldThrowCorrectException(bool allowRetry)
	{
		// Arrange
		const string dependencyName = "PaymentGateway";
		const string operationName = "ProcessPayment";

		// Act & Assert
		HangfireJobException exception = Assert.Throws<HangfireJobException>(() => HangfireJobException.ThrowDependencyUnavailable(dependencyName, operationName, allowRetry));

		Assert.Contains("Required dependency 'PaymentGateway' is unavailable", exception.Message);
		Assert.Equal(operationName, exception.OperationName);
		Assert.Equal(0, exception.EntityId);
		Assert.Equal(allowRetry, exception.AllowRetry);
	}

	[Theory]
	[InlineData(ThrowMethod.EntityNotFound)]
	[InlineData(ThrowMethod.ValidationFailed)]
	[InlineData(ThrowMethod.DependencyUnavailable)]
	public void ThrowMethods_DefaultAllowRetry_ShouldBeTrue(ThrowMethod method)
	{
		// Act & Assert
		HangfireJobException exception = method switch
		{
			ThrowMethod.EntityNotFound =>
				Assert.Throws<HangfireJobException>(() => HangfireJobException.ThrowEntityNotFound("Order", 789, "ProcessOrder")),
			ThrowMethod.ValidationFailed =>
				Assert.Throws<HangfireJobException>(() => HangfireJobException.ThrowValidationFailed("Invalid input", "ValidateInput", 999)),
			ThrowMethod.DependencyUnavailable =>
				Assert.Throws<HangfireJobException>(() => HangfireJobException.ThrowDependencyUnavailable("DatabaseConnection", "SaveData")),
			_ => throw new ArgumentException("Invalid method")
		};

		Assert.True(exception.AllowRetry);
	}

	[Fact]
	public void Message_WithAllComponents_ShouldFormatCorrectly()
	{
		// Arrange
		const string message = "Something went wrong";
		const string operationName = "ComplexOperation";
		const int entityId = 555;

		// Act
		HangfireJobException exception = new(message, operationName, entityId, allowRetry: false);

		// Assert
		string fullMessage = exception.Message;
		Assert.Contains("[Operation: ComplexOperation]", fullMessage);
		Assert.Contains("Something went wrong", fullMessage);
		Assert.Contains("(Entity ID: 555)", fullMessage);
		Assert.Contains("[PERMANENT FAILURE - NO RETRY]", fullMessage);
	}

	[Fact]
	public void Message_WithOnlyMessage_ShouldReturnBaseMessage()
	{
		// Arrange
		const string message = "Simple error";

		// Act
		HangfireJobException exception = new(message);

		// Assert
		Assert.Contains(message, exception.Message);
	}
}
