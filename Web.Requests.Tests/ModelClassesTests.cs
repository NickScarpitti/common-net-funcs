using System.Text.Json;
using CommonNetFuncs.Web.Requests;
using CommonNetFuncs.Web.Requests.Rest;
using MemoryPack;
using MessagePack;
using Microsoft.AspNetCore.Mvc;

namespace Web.Requests.Tests;

public enum SerializerType
{
	MessagePack,
	MemoryPack
}

public sealed class ModelClassesTests
{
	[Fact]
	public void AsyncIntString_CanBeCreated()
	{
		// Arrange & Act
		AsyncIntString asyncIntString = new()
		{
			AsyncInt = 42,
			AsyncDecimal = 3.14m,
			AsyncFloat = 2.71f,
			AsyncString = "test"
		};

		// Assert
		asyncIntString.AsyncInt.ShouldBe(42);
		asyncIntString.AsyncDecimal.ShouldBe(3.14m);
		asyncIntString.AsyncFloat.ShouldBe(2.71f);
		asyncIntString.AsyncString.ShouldBe("test");
	}

	[Theory]
	[InlineData(SerializerType.MessagePack)]
	[InlineData(SerializerType.MemoryPack)]
	public void AsyncIntString_CanBeSerialized(SerializerType serializerType)
	{
		// Arrange
		AsyncIntString original = new()
		{
			AsyncInt = serializerType == SerializerType.MessagePack ? 100 : 200,
			AsyncDecimal = serializerType == SerializerType.MessagePack ? 99.99m : 50.50m,
			AsyncFloat = serializerType == SerializerType.MessagePack ? 1.23f : 3.45f,
			AsyncString = serializerType == SerializerType.MessagePack ? "serialization test" : "memorypack test"
		};

		// Act
		AsyncIntString? deserialized;
		switch (serializerType)
		{
			case SerializerType.MessagePack:
				byte[] msgPackBytes = MessagePackSerializer.Serialize(original, cancellationToken: TestContext.Current.CancellationToken);
				deserialized = MessagePackSerializer.Deserialize<AsyncIntString>(msgPackBytes, cancellationToken: TestContext.Current.CancellationToken);
				break;
			case SerializerType.MemoryPack:
				byte[] memPackBytes = MemoryPackSerializer.Serialize(original);
				deserialized = MemoryPackSerializer.Deserialize<AsyncIntString>(memPackBytes);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(serializerType));
		}

		// Assert
		deserialized.ShouldNotBeNull();
		deserialized.AsyncInt.ShouldBe(original.AsyncInt);
		deserialized.AsyncDecimal.ShouldBe(original.AsyncDecimal);
		deserialized.AsyncFloat.ShouldBe(original.AsyncFloat);
		deserialized.AsyncString.ShouldBe(original.AsyncString);
	}

	[Fact]
	public void ProblemDetailsWithErrors_CanBeCreated()
	{
		// Arrange & Act
		ProblemDetailsWithErrors problemDetails = new()
		{
			Status = 400,
			Title = "Bad Request",
			Detail = "Validation failed"
		};
		problemDetails.Errors.Add("field1", new List<string> { "error1", "error2" });
		problemDetails.Errors.Add("field2", new List<string> { "error3" });

		// Assert
		problemDetails.Status.ShouldBe(400);
		problemDetails.Title.ShouldBe("Bad Request");
		problemDetails.Detail.ShouldBe("Validation failed");
		problemDetails.Errors.Count.ShouldBe(2);
		problemDetails.Errors["field1"].Count.ShouldBe(2);
		problemDetails.Errors["field2"].Count.ShouldBe(1);
	}

	[Fact]
	public void ProblemDetailsWithErrors_Errors_InitializedAsEmpty()
	{
		// Arrange & Act
		ProblemDetailsWithErrors problemDetails = new();

		// Assert
		problemDetails.Errors.ShouldNotBeNull();
		problemDetails.Errors.ShouldBeEmpty();
	}

	[Fact]
	public void ProblemDetailsWithErrors_PropertiesCanBeSerialized()
	{
		// Arrange
		ProblemDetailsWithErrors original = new()
		{
			Status = 404,
			Title = "Not Found",
			Detail = "Resource missing"
		};

		// Act - serialize only the ProblemDetails base properties
		string json = JsonSerializer.Serialize(original);
		ProblemDetailsWithErrors? deserialized = JsonSerializer.Deserialize<ProblemDetailsWithErrors>(json);

		// Assert
		deserialized.ShouldNotBeNull();
		deserialized.Status.ShouldBe(404);
		deserialized.Title.ShouldBe("Not Found");
		deserialized.Detail.ShouldBe("Resource missing");
		// Note: The Errors property is read-only and will be a new empty dictionary after deserialization
		deserialized.Errors.ShouldNotBeNull();
	}

	[Fact]
	public void MsgPackOptions_DefaultValues()
	{
		// Arrange & Act
		MsgPackOptions options = new();

		// Assert
		options.UseMsgPackCompression.ShouldBeFalse();
		options.UseMsgPackUntrusted.ShouldBeFalse();
	}

	[Fact]
	public void MsgPackOptions_CanSetProperties()
	{
		// Arrange & Act
		MsgPackOptions options = new()
		{
			UseMsgPackCompression = true,
			UseMsgPackUntrusted = true
		};

		// Assert
		options.UseMsgPackCompression.ShouldBeTrue();
		options.UseMsgPackUntrusted.ShouldBeTrue();
	}

	[Fact]
	public void RestObject_CanBeCreated()
	{
		// Arrange
		HttpResponseMessage response = new(System.Net.HttpStatusCode.OK);

		// Act
		RestObject<string> restObject = new()
		{
			Result = "test result",
			Response = response,
			Error = null
		};

		// Assert
		restObject.Result.ShouldBe("test result");
		restObject.Response.ShouldBe(response);
		restObject.Error.ShouldBeNull();
	}

	[Fact]
	public void RestObject_CanHaveNullValues()
	{
		// Arrange & Act
		RestObject<int?> restObject = new()
		{
			Result = null,
			Response = null,
			Error = "error message"
		};

		// Assert
		restObject.Result.ShouldBeNull();
		restObject.Response.ShouldBeNull();
		restObject.Error.ShouldBe("error message");
	}

	[Fact]
	public void RestObject_SupportsReferenceTypes()
	{
		// Arrange
		List<int> testList = new() { 1, 2, 3 };

		// Act
		RestObject<List<int>> restObject = new()
		{
			Result = testList
		};

		// Assert
		restObject.Result.ShouldBe(testList);
		restObject.Result!.Count.ShouldBe(3);
	}

	[Fact]
	public void RestObject_SupportsValueTypes()
	{
		// Arrange & Act
		RestObject<int> restObject = new()
		{
			Result = 42
		};

		// Assert
		restObject.Result.ShouldBe(42);
	}

	[Fact]
	public void StreamingRestObject_CanBeCreated()
	{
		// Arrange
		HttpResponseMessage response = new(System.Net.HttpStatusCode.OK);
		IAsyncEnumerable<string> asyncEnumerable = CreateAsyncEnumerable(new[] { "item1", "item2" });

		// Act
		StreamingRestObject<string> streamingObject = new()
		{
			Result = asyncEnumerable,
			Response = response
		};

		// Assert
		streamingObject.Result.ShouldBe(asyncEnumerable);
		streamingObject.Response.ShouldBe(response);
	}

	[Fact]
	public void StreamingRestObject_CanHaveNullValues()
	{
		// Arrange & Act
		StreamingRestObject<int> streamingObject = new()
		{
			Result = null,
			Response = null
		};

		// Assert
		streamingObject.Result.ShouldBeNull();
		streamingObject.Response.ShouldBeNull();
	}

	[Fact]
	public async Task StreamingRestObject_SupportsAsyncEnumeration()
	{
		// Arrange
		string[] expectedItems = { "a", "b", "c" };
		IAsyncEnumerable<string> asyncEnumerable = CreateAsyncEnumerable(expectedItems);

		StreamingRestObject<string> streamingObject = new()
		{
			Result = asyncEnumerable
		};

		// Act
		List<string?> results = new();
		await foreach (string? item in streamingObject.Result!)
		{
			results.Add(item);
		}

		// Assert
		results.ShouldBe(expectedItems);
	}

	[Fact]
	public void RestHelperConstants_HasCorrectHeaderNames()
	{
		// Assert
		RestHelperConstants.ContentTypeHeader.ShouldBe("Content-Type");
		RestHelperConstants.AcceptEncodingHeader.ShouldBe("Accept-Encoding");
		RestHelperConstants.AcceptHeader.ShouldBe("Accept");
	}

	[Fact]
	public void RestHelperConstants_EncodingHeaders_HaveCorrectValues()
	{
		// Assert - using Common.EncodingTypes values
		RestHelperConstants.NoEncodingHeader.Key.ShouldBe("Accept-Encoding");
		RestHelperConstants.NoEncodingHeader.Value.ShouldBe("identity");

		RestHelperConstants.BrotliEncodingHeader.Key.ShouldBe("Accept-Encoding");
		RestHelperConstants.BrotliEncodingHeader.Value.ShouldBe("br");

		RestHelperConstants.GzipEncodingHeader.Key.ShouldBe("Accept-Encoding");
		RestHelperConstants.GzipEncodingHeader.Value.ShouldBe("gzip");
	}

	[Fact]
	public void RestHelperConstants_ContentHeaders_HaveCorrectValues()
	{
		// Assert - using Common.ContentTypes values
		RestHelperConstants.MemPackContentHeader.Key.ShouldBe("Content-Type");
		RestHelperConstants.MemPackContentHeader.Value.ShouldBe("application/x-memorypack");

		RestHelperConstants.MsgPackContentHeader.Key.ShouldBe("Content-Type");
		RestHelperConstants.MsgPackContentHeader.Value.ShouldBe("application/x-msgpack");

		RestHelperConstants.JsonContentHeader.Key.ShouldBe("Content-Type");
		RestHelperConstants.JsonContentHeader.Value.ShouldBe("application/json");
	}

	[Fact]
	public void RestHelperConstants_AcceptHeaders_HaveCorrectValues()
	{
		// Assert
		RestHelperConstants.MemPackAcceptHeader.Key.ShouldBe("Accept");
		RestHelperConstants.MemPackAcceptHeader.Value.ShouldBe("application/x-memorypack");

		RestHelperConstants.MsgPackAcceptHeader.Key.ShouldBe("Accept");
		RestHelperConstants.MsgPackAcceptHeader.Value.ShouldBe("application/x-msgpack");

		RestHelperConstants.JsonAcceptHeader.Key.ShouldBe("Accept");
		RestHelperConstants.JsonAcceptHeader.Value.ShouldBe("application/json");
	}

	[Fact]
	public void RestHelperConstants_HeaderDictionaries_ContainCorrectPairs()
	{
		// Assert MemPack headers
		RestHelperConstants.MemPackHeaders.Count.ShouldBe(2);
		RestHelperConstants.MemPackHeaders.ContainsKey("Content-Type").ShouldBeTrue();
		RestHelperConstants.MemPackHeaders.ContainsKey("Accept").ShouldBeTrue();
		RestHelperConstants.MemPackHeaders["Content-Type"].ShouldBe("application/x-memorypack");
		RestHelperConstants.MemPackHeaders["Accept"].ShouldBe("application/x-memorypack");

		// Assert MsgPack headers
		RestHelperConstants.MsgPackHeaders.Count.ShouldBe(2);
		RestHelperConstants.MsgPackHeaders.ContainsKey("Content-Type").ShouldBeTrue();
		RestHelperConstants.MsgPackHeaders.ContainsKey("Accept").ShouldBeTrue();
		RestHelperConstants.MsgPackHeaders["Content-Type"].ShouldBe("application/x-msgpack");
		RestHelperConstants.MsgPackHeaders["Accept"].ShouldBe("application/x-msgpack");

		// Assert Json headers
		RestHelperConstants.JsonHeaders.Count.ShouldBe(2);
		RestHelperConstants.JsonHeaders.ContainsKey("Content-Type").ShouldBeTrue();
		RestHelperConstants.JsonHeaders.ContainsKey("Accept").ShouldBeTrue();
		RestHelperConstants.JsonHeaders["Content-Type"].ShouldBe("application/json");
		RestHelperConstants.JsonHeaders["Accept"].ShouldBe("application/json");

		// Assert JsonNoEncode headers (has encoding header too)
		RestHelperConstants.JsonNoEncodeHeaders.Count.ShouldBe(3);
		RestHelperConstants.JsonNoEncodeHeaders.ContainsKey("Content-Type").ShouldBeTrue();
		RestHelperConstants.JsonNoEncodeHeaders.ContainsKey("Accept").ShouldBeTrue();
		RestHelperConstants.JsonNoEncodeHeaders.ContainsKey("Accept-Encoding").ShouldBeTrue();
		RestHelperConstants.JsonNoEncodeHeaders["Accept-Encoding"].ShouldBe("identity");
	}

	[Fact]
	public void RestHelperConstants_EDelayBackoffType_HasExpectedValues()
	{
		// Assert - verify enum values exist
		RestHelperConstants.EDelayBackoffType constantValue = RestHelperConstants.EDelayBackoffType.Constant;
		RestHelperConstants.EDelayBackoffType linearValue = RestHelperConstants.EDelayBackoffType.Linear;
		RestHelperConstants.EDelayBackoffType exponentialValue = RestHelperConstants.EDelayBackoffType.Exponential;

		constantValue.ShouldBe(RestHelperConstants.EDelayBackoffType.Constant);
		linearValue.ShouldBe(RestHelperConstants.EDelayBackoffType.Linear);
		exponentialValue.ShouldBe(RestHelperConstants.EDelayBackoffType.Exponential);
	}

	// Helper method to create async enumerable for testing
	private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			await Task.Yield();
			yield return item;
		}
	}
}
