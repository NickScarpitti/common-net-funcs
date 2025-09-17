using MemoryPack;
using MessagePack;

namespace CommonNetFuncs.Web.Requests;

[MemoryPackable]
[MessagePackObject(true)]
public partial class AsyncIntString : Core.AsyncIntString
{
  //Adding ability to use with MessagePack and MemoryPack
}
