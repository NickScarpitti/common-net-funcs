using Polly;

namespace CommonNetFuncs.Web.Requests.Rest.Resilient;

public class ResiliencePipelineHandler(ResiliencePipeline<HttpResponseMessage> pipeline) : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await pipeline.ExecuteAsync(async (_) =>
               {
                   // Clone the request for potential retries since HttpRequestMessage can only be sent once
                   HttpRequestMessage clonedRequest = await CloneHttpRequestMessageAsync(request).ConfigureAwait(false);
                   return await base.SendAsync(clonedRequest, cancellationToken).ConfigureAwait(false);
               }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage original)
    {
        HttpRequestMessage clone = new(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        // Copy headers
        foreach (KeyValuePair<string, IEnumerable<string>> header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy properties (for .NET Core 2.1+)
        foreach (KeyValuePair<string, object?> property in original.Options)
        {
            clone.Options.TryAdd(property.Key, property.Value);
        }

        // Copy content if present
        if (original.Content != null)
        {
            byte[] contentBytes = await original.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            clone.Content = new ByteArrayContent(contentBytes);

            // Copy content headers
            foreach (KeyValuePair<string, IEnumerable<string>> header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}

