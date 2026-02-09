using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class ExceptionLocationTests
{
	private static Exception CreateExceptionFromMethod(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			return ex;
		}
		throw new InvalidOperationException("No exception was thrown.");
	}

	private static void ThrowFromInstanceMethod()
	{
		throw new InvalidOperationException("Test from instance method");
	}

	private static void ThrowFromStaticMethod()
	{
		throw new ArgumentException("Test from static method");
	}

	[Fact]
	public void GetLocationOfException_ReturnsExpectedFormat_ForInstanceMethod()
	{
		// Arrange
		Exception ex = CreateExceptionFromMethod(ThrowFromInstanceMethod);

		// Act
		string location = ex.GetLocationOfException();

		// Assert
		location.ShouldContain(nameof(ExceptionLocationTests));
		location.ShouldContain(nameof(ThrowFromInstanceMethod));
	}

	[Fact]
	public void GetLocationOfException_ReturnsExpectedFormat_ForStaticMethod()
	{
		// Arrange
		Exception ex = CreateExceptionFromMethod(ThrowFromStaticMethod);

		// Act
		string location = ex.GetLocationOfException();

		// Assert
		location.ShouldContain(nameof(ExceptionLocationTests));
		location.ShouldContain(nameof(ThrowFromStaticMethod));
	}

	[Fact]
	public void GetLocationOfException_ReturnsNullDot_WhenTargetSiteIsNull()
	{
		// Arrange
		Exception ex = new("No target site");

		// TargetSite is null for manually constructed exceptions

		// Act
		string location = ex.GetLocationOfException();

		// Assert
		location.ShouldBe("null.");
	}

	[Fact]
	public void GetLocationOfException_HandlesAnonymousMethod()
	{
		// Arrange
		Exception ex = CreateExceptionFromMethod(() =>
		{
			static void LocalFunction()
			{
				throw new ApplicationException("From local function");
			}

			LocalFunction();
		});

		// Act
		string location = ex.GetLocationOfException();

		// Assert
		location.ShouldContain(nameof(ExceptionLocationTests));
	}

	[Fact]
	public void GetLocationOfException_HandlesLambda()
	{
		// Arrange
		Exception ex = CreateExceptionFromMethod(() =>
		{
			static void lambda()
			{
				throw new ApplicationException("From lambda");
			}

			lambda();
		});

		// Act
		string location = ex.GetLocationOfException();

		// Assert
		location.ShouldContain(nameof(ExceptionLocationTests));
	}

	[Fact]
	public void GetLocationOfException_ReturnsNullPlusMethodName_WhenReflectedTypeIsNull()
	{
		// Arrange
		// Create a dynamic method which has no ReflectedType
		var dynamicMethod = new System.Reflection.Emit.DynamicMethod(
				"DynamicTestMethod",
				typeof(void),
				Type.EmptyTypes);

		var ilGenerator = dynamicMethod.GetILGenerator();
		ilGenerator.Emit(System.Reflection.Emit.OpCodes.Newobj,
				typeof(InvalidOperationException).GetConstructor([typeof(string)])!);
		ilGenerator.Emit(System.Reflection.Emit.OpCodes.Throw);

		Action action = (Action)dynamicMethod.CreateDelegate(typeof(Action));

		Exception? ex = null;
		try
		{
			action();
		}
		catch (Exception e)
		{
			ex = e;
		}

		// Act
		string location = ex!.GetLocationOfException();

		// Assert
		location.ShouldStartWith("null.");
		location.ShouldContain("DynamicTestMethod");
	}

	[Fact]
	public void ErrorLocationTemplate_HasExpectedValue()
	{
		// Assert
		ExceptionLocation.ErrorLocationTemplate.ShouldBe("Error in {ErrorLocation}");
	}
}
