namespace CommonNetFuncs.Web.Middleware;

public interface IResponseLoggingConfig
{
    double ThresholdInSeconds { get; }
}

/// <summary>
/// Config options for ResponseLoggingFilter
/// </summary>
public class ResponseLoggingConfig : IResponseLoggingConfig
{
    public double ThresholdInSeconds { get; set; }
}
