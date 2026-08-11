using System.Buffers;
using System.Runtime.CompilerServices;
using CommonNetFuncs.Web.Requests.MessagePack;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static Xunit.TestContext;

namespace Web.Requests.Tests;

public sealed class MessagePackStreamingTests
{
	#region Test Models

	[MessagePackObject]
	public class TestModel
	{
		[Key(0)]
		public int Id { get; set; }
		[Key(1)]
		public string Name { get; set; } = string.Empty;
	}

	#endregion

	#region MessagePackStreamingResult Tests - Controller Scenario

	[Fact]
	public async Task MessagePackStreamingResult_Controller_WithData_ReturnsSuccess()
	{
		// Arrange
		List<TestModel> testData =
		[
			new TestModel { Id = 1, Name = "Test1" },
			new TestModel { Id = 2, Name = "Test2" }
		];
		MessagePackStreamingResult<TestModel> result = new(testData.ToAsyncEnumerable());
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);

		// Act
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
		actionContext.HttpContext.Response.ContentType.ShouldBe("application/x-msgpack");

		// Verify the stream contains MessagePack data
		MemoryStream ms = (MemoryStream)actionContext.HttpContext.Response.Body;
		ms.Position = 0;

		using MessagePackStreamReader messagePackStreamReader = new(ms, leaveOpen: true);
		List<TestModel> readItems = [];

		while (await messagePackStreamReader.ReadAsync(Current.CancellationToken) is ReadOnlySequence<byte> msgPackData)
		{
			TestModel? item = MessagePackSerializer.Deserialize<TestModel>(msgPackData, MessagePackSerializerOptions.Standard, Current.CancellationToken);
			if (item != null)
			{
				readItems.Add(item);
			}
		}

		readItems.Count.ShouldBe(2);
		readItems[0].Id.ShouldBe(1);
		readItems[0].Name.ShouldBe("Test1");
		readItems[1].Id.ShouldBe(2);
		readItems[1].Name.ShouldBe("Test2");
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithNoData_ReturnsNoContent()
	{
		// Arrange
		List<TestModel> emptyData = [];
		MessagePackStreamingResult<TestModel> result = new(emptyData.ToAsyncEnumerable());
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);

		// Act
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
		actionContext.HttpContext.Response.ContentType.ShouldBe("application/x-msgpack");
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithCustomSuccessStatusCode_ReturnsCustomCode()
	{
		// Arrange
		List<TestModel> testData = [new TestModel { Id = 1, Name = "Test" }];
		MessagePackStreamingResult<TestModel> result = new(testData.ToAsyncEnumerable(), successStatusCode: StatusCodes.Status202Accepted);
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);

		// Act
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status202Accepted);
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithCustomEmptyStatusCode_ReturnsCustomCode()
	{
		// Arrange
		List<TestModel> emptyData = [];
		MessagePackStreamingResult<TestModel> result = new(emptyData.ToAsyncEnumerable(), emptyStatusCode: StatusCodes.Status404NotFound);
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);

		// Act
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithCompressedOptions_SerializesWithCompression()
	{
		// Arrange
		List<TestModel> testData = [new TestModel { Id = 1, Name = "Test" }];
		MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
		MessagePackStreamingResult<TestModel> result = new(testData.ToAsyncEnumerable(), options);
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);

		// Act
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
		MemoryStream ms = (MemoryStream)actionContext.HttpContext.Response.Body;
		ms.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithNullData_ThrowsArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => new MessagePackStreamingResult<TestModel>(null!));
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithNullContext_ThrowsArgumentNullException()
	{
		// Arrange
		List<TestModel> testData = [new TestModel { Id = 1, Name = "Test" }];
		MessagePackStreamingResult<TestModel> result = new(testData.ToAsyncEnumerable());

		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(async () => await result.ExecuteResultAsync(null!));
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithExceptionDuringIteration_Returns500IfNotStarted()
	{
		// Arrange
		async IAsyncEnumerable<TestModel> GetDataWithError()
		{
			yield return new TestModel { Id = 1, Name = "Test1" };
			await Task.Delay(1);
			throw new InvalidOperationException("Test exception");
		}

		MessagePackStreamingResult<TestModel> result = new(GetDataWithError());
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);

		// Act & Assert
		await Should.ThrowAsync<InvalidOperationException>(async () => await result.ExecuteResultAsync(actionContext));

		// Response should be 500 if not started
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithCancellation_StopsStreamingWithoutThrowingAndSetsClientClosedStatus()
	{
		// Arrange
		CancellationTokenSource cts = new();
		int itemsYielded = 0;
		async IAsyncEnumerable<TestModel> GetDataSlowly()
		{
			for (int i = 0; i < 10; i++)
			{
				await Task.Delay(10, Current.CancellationToken);
				itemsYielded++;
				yield return new TestModel { Id = i, Name = $"Test{i}" };
				if (i == 2)
				{
					cts.Cancel();
				}
			}
		}

		MessagePackStreamingResult<TestModel> result = new(GetDataSlowly());
		ActionContext actionContext = CreateActionContext(cts.Token);

		// Act - cancellation should be handled internally, not thrown
		await result.ExecuteResultAsync(actionContext);

		// Assert - streaming stopped shortly after cancellation instead of running through all 10 items
		itemsYielded.ShouldBeLessThan(10);
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status499ClientClosedRequest);
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithCancellation_PropagatesTokenToAsyncEnumerable()
	{
		// Arrange
		CancellationTokenSource cts = new();
		int itemsProduced = 0;

		// Only completes early if the cancellation token passed to the enumerator is signaled (via WithCancellation).
		async IAsyncEnumerable<TestModel> GetDataObservingCancellation([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			for (int i = 0; i < 5; i++)
			{
				itemsProduced++;
				yield return new TestModel { Id = i, Name = $"Test{i}" };
				await Task.Delay(Timeout.Infinite, cancellationToken);
			}
		}

		MessagePackStreamingResult<TestModel> result = new(GetDataObservingCancellation(Current.CancellationToken));
		ActionContext actionContext = CreateActionContext(cts.Token);

		Task executeTask = result.ExecuteResultAsync(actionContext);
		await Task.Delay(50, Current.CancellationToken);
		cts.Cancel();

		// Act - if the token isn't propagated to the enumerable, this will hang until the test times out
		Task completedTask = await Task.WhenAny(executeTask, Task.Delay(TimeSpan.FromSeconds(5), Current.CancellationToken));

		// Assert
		completedTask.ShouldBe(executeTask, "ExecuteResultAsync did not complete; the cancellation token was not propagated to the IAsyncEnumerable");
		await executeTask;
		itemsProduced.ShouldBe(1);
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status499ClientClosedRequest);
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithAlreadyCancelledToken_DoesNotThrowAndSetsClientClosedStatus()
	{
		// Arrange
		List<TestModel> testData =
		[
			new TestModel { Id = 1, Name = "Test1" },
			new TestModel { Id = 2, Name = "Test2" }
		];
		MessagePackStreamingResult<TestModel> result = new(testData.ToAsyncEnumerable());
		ActionContext actionContext = CreateActionContext(new CancellationToken(canceled: true));

		// Act
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status499ClientClosedRequest);
	}

	[Fact]
	public async Task MessagePackStreamingResult_WithUnrelatedOperationCanceledException_Returns500AndRethrows()
	{
		// Arrange - throws OperationCanceledException that is unrelated to the request's own RequestAborted token
		async IAsyncEnumerable<TestModel> GetDataWithUnrelatedCancellation()
		{
			yield return new TestModel { Id = 1, Name = "Test1" };
			await Task.Delay(1, Current.CancellationToken);
			throw new OperationCanceledException("Unrelated cancellation");
		}

		MessagePackStreamingResult<TestModel> result = new(GetDataWithUnrelatedCancellation());
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);

		// Act & Assert - since actionContext's RequestAborted token was never cancelled, this should not be treated as a client disconnect
		await Should.ThrowAsync<OperationCanceledException>(async () => await result.ExecuteResultAsync(actionContext));
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
	}

	#endregion

	#region MessagePackStreamingExtensions Tests - Controller Extension Methods

	[Fact]
	public void StreamMessagePack_ControllerExtension_WithData_ReturnsMessagePackStreamingResult()
	{
		// Arrange
		FakeController controller = new();
		List<TestModel> testData = [new TestModel { Id = 1, Name = "Test" }];

		// Act
		MessagePackStreamingResult<TestModel> result = controller.StreamMessagePack(testData.ToAsyncEnumerable());

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeOfType<MessagePackStreamingResult<TestModel>>();
	}

	[Fact]
	public void StreamMessagePack_ControllerExtension_WithOptions_PassesOptionsToResult()
	{
		// Arrange
		FakeController controller = new();
		List<TestModel> testData = [new TestModel { Id = 1, Name = "Test" }];
		MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

		// Act
		MessagePackStreamingResult<TestModel> result = controller.StreamMessagePack(testData.ToAsyncEnumerable(), options);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void StreamMessagePack_ControllerExtension_WithCustomStatusCodes_PassesCodesToResult()
	{
		// Arrange
		FakeController controller = new();
		List<TestModel> testData = [new TestModel { Id = 1, Name = "Test" }];

		// Act
		MessagePackStreamingResult<TestModel> result = controller.StreamMessagePack(
			testData.ToAsyncEnumerable(),
			successStatusCode: StatusCodes.Status202Accepted,
			emptyStatusCode: StatusCodes.Status404NotFound);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void StreamMessagePack_ControllerExtension_WithNullData_ThrowsArgumentNullException()
	{
		// Arrange
		FakeController controller = new();

		// Act & Assert
		Should.Throw<ArgumentNullException>(() => controller.StreamMessagePack<TestModel>(null!));
	}

	#endregion

	#region MessagePackStreaming Tests - Minimal API Static Methods

	[Fact]
	public async Task MessagePackStreaming_MinimalApi_WithData_ReturnsSuccess()
	{
		// Arrange
		List<TestModel> testData =
		[
			new TestModel { Id = 1, Name = "MinimalApi1" },
			new TestModel { Id = 2, Name = "MinimalApi2" },
			new TestModel { Id = 3, Name = "MinimalApi3" }
		];

		// Act - Using static helper as you would in a Minimal API
		MessagePackStreamingResult<TestModel> result = MessagePackStreaming.Stream(testData.ToAsyncEnumerable());
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
		actionContext.HttpContext.Response.ContentType.ShouldBe("application/x-msgpack");

		// Verify the stream contains MessagePack data
		MemoryStream ms = (MemoryStream)actionContext.HttpContext.Response.Body;
		ms.Position = 0;

		using MessagePackStreamReader messagePackStreamReader = new(ms, leaveOpen: true);
		List<TestModel> readItems = [];

		while (await messagePackStreamReader.ReadAsync(Current.CancellationToken) is ReadOnlySequence<byte> msgPackData)
		{
			TestModel? item = MessagePackSerializer.Deserialize<TestModel>(msgPackData, MessagePackSerializerOptions.Standard, Current.CancellationToken);
			if (item != null)
			{
				readItems.Add(item);
			}
		}

		readItems.Count.ShouldBe(3);
		readItems[0].Name.ShouldBe("MinimalApi1");
		readItems[1].Name.ShouldBe("MinimalApi2");
		readItems[2].Name.ShouldBe("MinimalApi3");
	}

	[Fact]
	public async Task MessagePackStreaming_MinimalApi_WithEmptyData_ReturnsNoContent()
	{
		// Arrange
		List<TestModel> emptyData = [];

		// Act - Using static helper as you would in a Minimal API
		MessagePackStreamingResult<TestModel> result = MessagePackStreaming.Stream(emptyData.ToAsyncEnumerable());
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
	}

	[Fact]
	public async Task MessagePackStreaming_MinimalApi_WithCompression_SerializesCorrectly()
	{
		// Arrange
		List<TestModel> testData = [new TestModel { Id = 100, Name = "CompressedMinimalApi" }];
		MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

		// Act - Using static helper with compression as you would in a Minimal API
		MessagePackStreamingResult<TestModel> result = MessagePackStreaming.Stream(testData.ToAsyncEnumerable(), options);
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
		MemoryStream ms = (MemoryStream)actionContext.HttpContext.Response.Body;
		ms.Length.ShouldBeGreaterThan(0);
	}

	[Fact]
	public async Task MessagePackStreaming_MinimalApi_WithCustomStatusCodes_ReturnsCustomCodes()
	{
		// Arrange
		List<TestModel> testData = [new TestModel { Id = 1, Name = "Test" }];

		// Act - Using static helper with custom status codes
		MessagePackStreamingResult<TestModel> result = MessagePackStreaming.Stream(testData.ToAsyncEnumerable(), successStatusCode: StatusCodes.Status202Accepted, emptyStatusCode: StatusCodes.Status404NotFound);

		ActionContext actionContext = CreateActionContext(Current.CancellationToken);
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status202Accepted);
	}

	[Fact]
	public async Task MessagePackStreaming_MinimalApi_WithLargeDataset_StreamsEfficiently()
	{
		// Arrange - Simulate a large dataset
		async IAsyncEnumerable<TestModel> GetLargeDataset()
		{
			for (int i = 0; i < 1000; i++)
			{
				await Task.Yield();
				yield return new TestModel { Id = i, Name = $"Item{i}" };
			}
		}

		// Act - Using static helper for large dataset streaming
		MessagePackStreamingResult<TestModel> result = MessagePackStreaming.Stream(GetLargeDataset());
		ActionContext actionContext = CreateActionContext(Current.CancellationToken);
		await result.ExecuteResultAsync(actionContext);

		// Assert
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);

		// Verify we can read all items back
		MemoryStream ms = (MemoryStream)actionContext.HttpContext.Response.Body;
		ms.Position = 0;

		using MessagePackStreamReader messagePackStreamReader = new(ms, leaveOpen: true);
		int count = 0;

		while (await messagePackStreamReader.ReadAsync(Current.CancellationToken) is ReadOnlySequence<byte> msgPackData)
		{
			TestModel? item = MessagePackSerializer.Deserialize<TestModel>(msgPackData, MessagePackSerializerOptions.Standard, Current.CancellationToken);
			if (item != null)
			{
				count++;
			}
		}

		count.ShouldBe(1000);
	}

	[Fact]
	public void MessagePackStreaming_MinimalApi_WithNullData_ThrowsArgumentNullException()
	{
		// Act & Assert
		Should.Throw<ArgumentNullException>(() => MessagePackStreaming.Stream<TestModel>(null!));
	}

	#endregion

	#region Integration Tests - Both Controller and Minimal API Patterns

	[Theory]
	[InlineData(true)]  // Controller pattern
	[InlineData(false)] // Minimal API pattern
	public async Task MessagePackStreaming_BothPatterns_ProduceSameOutput(bool useControllerPattern)
	{
		// Arrange
		List<TestModel> testData =
		[
			new TestModel { Id = 1, Name = "Test1" },
			new TestModel { Id = 2, Name = "Test2" }
		];

		MessagePackStreamingResult<TestModel> result;
		if (useControllerPattern)
		{
			// Controller pattern
			FakeController controller = new();
			result = controller.StreamMessagePack(testData.ToAsyncEnumerable());
		}
		else
		{
			// Minimal API pattern
			result = MessagePackStreaming.Stream(testData.ToAsyncEnumerable());
		}

		ActionContext actionContext = CreateActionContext(Current.CancellationToken);

		// Act
		await result.ExecuteResultAsync(actionContext);

		// Assert - Both patterns should produce identical results
		actionContext.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
		actionContext.HttpContext.Response.ContentType.ShouldBe("application/x-msgpack");

		MemoryStream ms = (MemoryStream)actionContext.HttpContext.Response.Body;
		ms.Position = 0;

		using MessagePackStreamReader messagePackStreamReader = new(ms, leaveOpen: true);
		List<TestModel> readItems = [];

		while (await messagePackStreamReader.ReadAsync(Current.CancellationToken) is ReadOnlySequence<byte> msgPackData)
		{
			TestModel? item = MessagePackSerializer.Deserialize<TestModel>(msgPackData, MessagePackSerializerOptions.Standard, Current.CancellationToken);
			if (item != null)
			{
				readItems.Add(item);
			}
		}

		readItems.Count.ShouldBe(2);
		readItems[0].Id.ShouldBe(1);
		readItems[1].Id.ShouldBe(2);
	}

	#endregion

	#region Helper Methods

	private static ActionContext CreateActionContext(CancellationToken cancellationToken = default)
	{
		DefaultHttpContext httpContext = new();
		httpContext.Response.Body = new MemoryStream();
		httpContext.RequestAborted = cancellationToken;

		return new ActionContext
		{
			HttpContext = httpContext,
			RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
			ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
		};
	}

	private class FakeController : ControllerBase
	{
	}

	#endregion
}

public static class AsyncEnumerableExtensions
{
	public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
	{
		foreach (T item in source)
		{
			await Task.Yield();
			yield return item;
		}
	}
}
