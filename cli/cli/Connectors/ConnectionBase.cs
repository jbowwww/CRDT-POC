using System.IO;
using System.Net.Sockets;

namespace Aemo.Connectors;

public abstract class ConnectionBase : IConnection
{
  public IConnector Connector { get; protected set; } = null!;
  public virtual string ConnectionId { get; init; } = string.Empty;
  public virtual Socket Socket { get; init; } = null!;
  public virtual NetworkStream Stream { get; init; } = null!;
  public override string ToString()
   => $"[{GetType().FullName} ConnectionId={this.ConnectionId} Socket={Socket} TxStream={Stream}]";
}
