using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CommonNetFuncs.Web.Api;
using FakeItEasy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Web.Api.Tests;

public sealed class ValidationEndpointFilterTests
{
	private readonly IServiceProviderIsService serviceChecker;
	private readonly ValidationEndpointFilter sut;

	public ValidationEndpointFilterTests()
	{
		serviceChecker = A.Fake<IServiceProviderIsService>();
		A.CallTo(() => serviceChecker.IsService(A<Type>.Ignored)).Returns(false);
		sut = new ValidationEndpointFilter(serviceChecker);
	}

	// --- Helper types ---

	private enum TestEnum { A, B }

	private sealed class ValidModel
	{
		[Required]
		public string Name { get; set; } = "valid";
	}

	private sealed class InvalidModel
	{
		[Required]
		public string? Name { get; set; } = null;
	}

	private sealed class ObjectLevelInvalidModel : IValidatableObject
	{
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			yield return new ValidationResult("Object-level error"); // no member names → null-coalescing ?? string.Empty branch
		}
	}

	private sealed class NullErrorMessageInvalidModel : IValidatableObject
	{
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			yield return new ValidationResult(null, ["SomeField"]); // null error message → vr.ErrorMessage ?? string.Empty branch
		}
	}

	private sealed class RegisteredService { }

	private sealed class TestClaimsPrincipal : ClaimsPrincipal { } // subclass not in SkippedTypes → hits IsAssignableTo(ClaimsPrincipal)

	private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext, params object?[] args)
		: EndpointFilterInvocationContext
	{
		public override HttpContext HttpContext { get; } = httpContext;
		public override IList<object?> Arguments { get; } = new List<object?>(args);
		public override T GetArgument<T>(int index) => (T)Arguments[index]!;
	}

	private static EndpointFilterInvocationContext CreateContext(params object?[] args)
	{
		DefaultHttpContext httpContext = new()
		{
			RequestServices = new ServiceCollection().BuildServiceProvider()
		};
		return new TestEndpointFilterInvocationContext(httpContext, args);
	}

	private static EndpointFilterDelegate Next(out bool called)
	{
		bool wasCalled = false;
		called = false;
		EndpointFilterDelegate del = _ => { wasCalled = true; return ValueTask.FromResult<object?>("next"); };
		called = wasCalled;
		return del;
	}

	// --- InvokeAsync: skipped argument types ---

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsNull_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext((object?)null);
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsPrimitive_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(42); // int → IsPrimitive = true
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsEnum_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(TestEnum.A); // enum → IsEnum = true
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsString_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext("hello");
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsDecimal_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(1.5m);
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsGuid_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(Guid.NewGuid());
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsDateTime_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(DateTime.Now);
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsDateTimeOffset_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(DateTimeOffset.Now);
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsDateOnly_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(DateOnly.FromDateTime(DateTime.Now));
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsTimeOnly_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(TimeOnly.FromDateTime(DateTime.Now));
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsCancellationToken_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new CancellationToken()); // exact type is in SkippedTypes
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsClaimsPrincipal_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new ClaimsPrincipal()); // exact type is in SkippedTypes
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsHttpContextSubclass_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new DefaultHttpContext()); // not in SkippedTypes, but IsAssignableTo(HttpContext)
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsClaimsPrincipalSubclass_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new TestClaimsPrincipal()); // not in SkippedTypes, but IsAssignableTo(ClaimsPrincipal)
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsRegisteredDiService_CallsNext()
	{
		A.CallTo(() => serviceChecker.IsService(typeof(RegisteredService))).Returns(true);
		EndpointFilterInvocationContext ctx = CreateContext(new RegisteredService());
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	// --- InvokeAsync: validation outcomes ---

	[Fact]
	public async Task InvokeAsync_WhenArgumentIsValidModel_CallsNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new ValidModel());
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBe("next");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentFailsPropertyValidation_ReturnsValidationProblem()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new InvalidModel()); // [Required] Name is null
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBeOfType<ValidationProblem>();
		ValidationProblem vp = (ValidationProblem)result!;
		vp.ProblemDetails.Errors.ShouldContainKey("Name");
	}

	[Fact]
	public async Task InvokeAsync_WhenArgumentFailsObjectLevelValidation_ReturnsValidationProblemWithEmptyKey()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new ObjectLevelInvalidModel()); // IValidatableObject with no MemberNames → ?? string.Empty
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBeOfType<ValidationProblem>();
		ValidationProblem vp = (ValidationProblem)result!;
		vp.ProblemDetails.Errors.ShouldContainKey(string.Empty);
	}

	[Fact]
	public async Task InvokeAsync_WhenValidationResultHasNullErrorMessage_ReturnsValidationProblemWithEmptyMessage()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new NullErrorMessageInvalidModel()); // null error message → ErrorMessage ?? string.Empty
		object? result = await sut.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("next"));
		result.ShouldBeOfType<ValidationProblem>();
		ValidationProblem vp = (ValidationProblem)result!;
		vp.ProblemDetails.Errors["SomeField"].ShouldContain(string.Empty);
	}

	[Fact]
	public async Task InvokeAsync_StopsOnFirstInvalidArgument_DoesNotCallNext()
	{
		EndpointFilterInvocationContext ctx = CreateContext(new InvalidModel(), new ValidModel());
		bool nextCalled = false;
		object? result = await sut.InvokeAsync(ctx, _ => { nextCalled = true; return ValueTask.FromResult<object?>("next"); });
		result.ShouldBeOfType<ValidationProblem>();
		nextCalled.ShouldBeFalse();
	}

	// --- Extension method tests ---

	[Fact]
	public void WithValidation_RouteGroupBuilder_ReturnsBuilder()
	{
		using WebApplication app = WebApplication.Create([]);
		RouteGroupBuilder group = app.MapGroup("/test");
		RouteGroupBuilder result = group.WithValidation();
		result.ShouldBeSameAs(group);
	}

	[Fact]
	public void WithValidation_RouteHandlerBuilder_ReturnsBuilder()
	{
		using WebApplication app = WebApplication.Create([]);
		RouteHandlerBuilder handler = app.MapGet("/test", () => "test");
		RouteHandlerBuilder result = handler.WithValidation();
		result.ShouldBeSameAs(handler);
	}
}
