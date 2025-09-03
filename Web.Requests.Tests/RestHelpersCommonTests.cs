using System.Net;
using System.Text;
using CommonNetFuncs.Web.Requests;

namespace Web.Requests.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly

public sealed class RestHelpersCommonTests
{
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

    [Fact]
    public async Task RestRequest_DelegatesToStaticExtension()
    {
        FakeHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler);
        RestHelpersCommon restHelpers = new(httpClient);
        RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"result\"", Encoding.UTF8, "application/json")
        };

        string? result = await restHelpers.RestRequest<string, string>(options);

        result.ShouldBe("result");
    }

    public class StringWrapper
    {
        public string? Value { get; set; }
    }

    [Fact]
    public async Task StreamingRestRequest_DelegatesToStaticExtension()
    {
        FakeHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler);
        RestHelpersCommon restHelpers = new(httpClient);
        RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

        const string json = "[{\"Value\":\"a\"},{\"Value\":\"b\"}]";
        StringContent content = new(json, Encoding.UTF8, "application/json");
        content.Headers.ContentLength = Encoding.UTF8.GetByteCount(json);
        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content
        };

        List<string?> results = new();
        await foreach (StringWrapper? item in restHelpers.StreamingRestRequest<StringWrapper, string>(options))
        {
            results.Add(item?.Value);
        }
        results.ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task RestRequestObject_DelegatesToStaticExtension()
    {
        FakeHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler);
        RestHelpersCommon restHelpers = new(httpClient);
        RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"foo\"", Encoding.UTF8, "application/json")
        };

        RestObject<string> result = await restHelpers.RestRequestObject<string, string>(options);

        result.Result.ShouldBe("foo");
        result.Response.ShouldNotBeNull();
    }

    [Fact]
    public async Task StreamingRestRequestObject_DelegatesToStaticExtension()
    {
        FakeHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler);
        RestHelpersCommon restHelpers = new(httpClient);
        RequestOptions<string> options = new() { Url = "http://test", HttpMethod = HttpMethod.Get };

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[\"x\",\"y\"]", Encoding.UTF8, "application/json")
        };

        StreamingRestObject<string> result = await restHelpers.StreamingRestRequestObject<string, string>(options);

        List<string?> items = new();
        await foreach (string? item in result.Result!)
        {
            items.Add(item);
        }
        items.ShouldBe(["x", "y"]);
    }
}

#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
