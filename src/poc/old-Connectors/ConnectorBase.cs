using System;
using System.Linq;
using System.Threading.Tasks;
using cli.Connectors;
using Ycs;

namespace Aemo.Connectors;

public abstract class ConnectorBase<TConnection, TConnectorOptions>
 : IConnector<TConnection, TConnectorOptions>
  where TConnection : IConnection
  where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
{
  public virtual TConnectorOptions Options { get; init; } = new();
  public abstract string ConnectionId { get; }
  public ConnectionDictionary<IConnection> Connections { get; } = new();
  public ConnectionStatus Status { get; set; }
  public virtual bool IsInit => Status == ConnectionStatus.Init;
  public virtual bool IsConnected => Status <= ConnectionStatus.Partitioned;
  public virtual bool IsPartitioned => Status == ConnectionStatus.Partitioned;
  public virtual bool IsError => Status == ConnectionStatus.Error;
  public virtual bool IsDisconnected => Status >= ConnectionStatus.Disconnecting;
  public virtual ConnectedDocument Document { get; init; } = null!;

  public override string ToString()
   => $"[{GetType().Name} Id=\"{ConnectionId}\" Status={Status} Document.Name={Document?.Name} Connections={Connections}]";

  ~ConnectorBase()
  {
    Dispose();
  }

  public void Dispose()
  {
    Disconnect();
    System.GC.SuppressFinalize(this);
  }

  public abstract Task Connect();

  public abstract void Disconnect();

  public void Receive(string connectionId, byte[] data)
  {
    Console.WriteLine($"Receive(): Status={Status} Connections.Count={Connections.Count} Options={this}\n\tconnectionId={connectionId}\n\tdata.Length={data.Length}");
    if (Connections.TryGetValue(connectionId, out IConnection? connection))
    {
      if (IsConnected)
      {
        SyncProtocol.ReadUpdate(connection?.Stream, Document, this);
      }
    }
    else
    {
      throw new ArgumentException($"Could not find connectionId=\"{connectionId}\" in Connections.Count={Connections.Count}");
    }
    Console.WriteLine($"Receive(): End");
  }

  public void Send(string connectionId, byte[] data)
  {
    Console.WriteLine($"Send(): Status={Status} Connections.Count={Connections.Count} Options={this}\n\tconnectionId={connectionId}\n\tdata={SyncProtocol.EncodeBytes(data)}");
    if (Connections.TryGetValue(connectionId, out IConnection? connection))
    {
      if (IsConnected)
      {
        SyncProtocol.WriteUpdate(connection?.Stream, data);
      }
    }
    else
    {
      throw new ArgumentException($"Could not find connectionId=\"{connectionId}\" in Connections.Count={Connections.Count}");
    }
    Console.WriteLine($"Send(): End()");
  }

  public void Broadcast(byte[] data)
  {
    Console.WriteLine($"Broadcast(): Status={Status} Connections.Count={Connections.Count} Options={this}\n\tdata={SyncProtocol.EncodeBytes(data)}");
    if (IsConnected)
    {
      Connections?.AsParallel<IConnection>().ForAll(
        connection => SyncProtocol.WriteUpdate(connection.Stream, data));
    }
    Console.WriteLine($"Broadcast(): End");
  }

  public void Broadcast(Action<IConnection> streamAction)
  {
    Console.WriteLine($"Broadcast(): streamAction={streamAction}");
    Connections.AsParallel<IConnection>().ForAll(streamAction.Invoke);
  }
}
