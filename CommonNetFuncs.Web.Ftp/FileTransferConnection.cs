namespace CommonNetFuncs.Web.Ftp;

public sealed class FileTransferConnection
{
  public string HostName { get; set; } = string.Empty;

  public string UserName { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;

  public string ConnectionProtocol { get; set; } = string.Empty;

  public int Port { get; set; }

  public uint BufferSize { get; set; }
}
