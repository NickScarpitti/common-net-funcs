﻿using System.Net;
using System.Text;
using CommonNetFuncs.Web.Requests;

namespace Web.Requests.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly

public sealed class RestHelpersStaticTests
{
    [Theory]
    [InlineData("GET", null)]
    [InlineData("POST", "body")]
    public async Task RestRequest_ReturnsExpectedResult(string methodString, string? body)
    {
        HttpMethod method = new(methodString);
        FakeHttpMessageHandler handler = new();
        HttpClient client = new(handler);
        RequestOptions<string> options = new()
        {
            Url = "http://test",
            HttpMethod = method,
            BodyObject = body,
            Timeout = 1,
            LogQuery = true,
            LogBody = true
        };

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"result\"", Encoding.UTF8, "application/json")
        };

        string? result = await client.RestRequest<string, string>(options);

        result.ShouldBe("result");
    }

    [Fact]
    public async Task RestRequest_HandlesTaskCanceledException()
    {
        FakeHttpMessageHandler handler = new() { ThrowOnSend = new TaskCanceledException("Canceled!") };
        HttpClient client = new(handler);
        RequestOptions<string> options = new()
        {
            Url = "http://test",
            HttpMethod = HttpMethod.Get,
            ExpectTaskCancellation = true
        };

        string? result = await client.RestRequest<string, string>(options);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task RestRequest_HandlesGeneralException()
    {
        FakeHttpMessageHandler handler = new() { ThrowOnSend = new InvalidOperationException("fail") };
        HttpClient client = new(handler);
        RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

        string? result = await client.RestRequest<string, string>(options);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task RestObjectRequest_ReturnsRestObject()
    {
        FakeHttpMessageHandler handler = new();
        HttpClient client = new(handler);
        RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"foo\"", Encoding.UTF8, "application/json")
        };

        RestObject<string> result = await client.RestObjectRequest<string, string>(options);

        result.Result.ShouldBe("foo");
        result.Response.ShouldNotBeNull();
    }

    [Fact]
    public async Task StreamingRestRequest_YieldsResults()
    {
        FakeHttpMessageHandler handler = new();
        HttpClient client = new(handler);
        RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[\"a\",\"b\"]", Encoding.UTF8, "application/json")
        };

        List<string?> results = new();
        await foreach (string? item in client.StreamingRestRequest<string, string>(options))
        {
            results.Add(item);
        }

        results.ShouldBe(["a", "b"]);
    }

    [Fact]
    public void GetChunkingParameters_ReturnsExpectedValues()
    {
        (int itemsPerChunk, int numberOfChunks) = RestHelpersStatic.GetChunkingParameters(1000000, 100, 8192);
        itemsPerChunk.ShouldBeGreaterThan(0);
        numberOfChunks.ShouldBeGreaterThan(0);
    }

    // Additional tests for AddContent, AttachHeaders, HandleResponse, etc. would go here.
    // For brevity, only the main public API is covered, but for 100% coverage, all internal methods should be tested similarly.

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage? Response { get; set; }

        public Exception? ThrowOnSend { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ThrowOnSend is not null)
            {
                throw ThrowOnSend;
            }

            return Task.FromResult(Response ?? new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
