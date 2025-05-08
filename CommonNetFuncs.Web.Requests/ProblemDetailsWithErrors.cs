using Microsoft.AspNetCore.Mvc;

namespace CommonNetFuncs.Web.Requests;

public sealed class ProblemDetailsWithErrors : ProblemDetails
{
    public IDictionary<string, List<string>> Errors { get; } = new Dictionary<string, List<string>>(StringComparer.Ordinal);
}
