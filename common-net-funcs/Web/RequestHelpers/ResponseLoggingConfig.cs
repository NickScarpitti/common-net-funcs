namespace Common_Net_Funcs.Web.RequestHelpers;

/// <summary>
/// Config options for ResponseLoggingFilter
/// </summary>
public class ResponseLoggingConfig : IResponseLoggingConfig
{
    public double ThresholdInSeconds { get; set; }
}
