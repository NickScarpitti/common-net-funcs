using CommonNetFuncs.Core;
using System.Reflection.Emit;

namespace Core.Tests;

#pragma warning disable CRR1000 // The name does not correspond to naming conventions

public class ExceptionLocationTests
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

    private static void ThrowFromInstanceMethod() { throw new InvalidOperationException("Test from instance method"); }

    private static void ThrowFromStaticMethod() { throw new ArgumentException("Test from static method"); }

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
        location.ShouldBe(".");
    }

    [Fact]
    public void GetLocationOfException_HandlesAnonymousMethod()
    {
        // Arrange
        Exception ex = CreateExceptionFromMethod(() =>
        {
            static void LocalFunction() { throw new ApplicationException("From local function"); }

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
            static void lambda() { throw new ApplicationException("From lambda"); }

            lambda();
        });

        // Act
        string location = ex.GetLocationOfException();

        // Assert
        location.ShouldContain(nameof(ExceptionLocationTests));
    }
}

#pragma warning restore CRR1000 // The name does not correspond to naming conventions
