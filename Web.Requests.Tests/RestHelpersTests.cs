using System.Reflection;
using CommonNetFuncs.Web.Requests.Rest;

namespace Web.Requests.Tests;

public sealed class RestHelpersTests
{
    [Fact]
    public void RestHelpers_InitializesHttpClient()
    {
        Type type = typeof(RestHelpers);
        FieldInfo? clientField = type.GetField("client", BindingFlags.Static | BindingFlags.NonPublic);
        object? value = clientField?.GetValue(null);

        value.ShouldBeOfType<HttpClient>();
        ((HttpClient)value!).Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
    }
}
