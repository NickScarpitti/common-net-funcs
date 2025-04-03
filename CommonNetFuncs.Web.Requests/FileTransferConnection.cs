

namespace CommonNetFuncs.Web.Requests;
public class FileTransferConnection
{
    public string HostName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConnectionProtocol { get; set; } = string.Empty;
    public int Port { get; set; }
}
