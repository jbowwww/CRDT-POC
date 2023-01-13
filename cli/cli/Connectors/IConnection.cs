using System.IO;
using System.Net.Sockets;

namespace Aemo.Connectors;

public interface IConnection
{
  public string ConnectionId { get; init; }

  public Socket Socket { get; init; }

  public NetworkStream Stream { get; init; }
}
