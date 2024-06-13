namespace Common_Net_Funcs.Web.RequestHelpers;

/// <summary>
/// Interface for configuring ResponseLoggingFilter
/// </summary>
public interface IResponseLoggingConfig
{
    double ThresholdInSeconds { get; }
}
