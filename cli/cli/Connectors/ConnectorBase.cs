using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ycs;

namespace Aemo.Connectors;

public enum ConnectorStatus
{
  Init = 0,
  Connecting,
  Connected,
  Partitioned,
  Error,
  Disconnecting,
  Disconnected
};

public abstract class ConnectorBase<TConnection, TConnectorOptions> : IConnector<TConnection, TConnectorOptions>, IConnector
  where TConnection : ConnectionBase, IConnection, new()
  where TConnectorOptions : IConnectorOptions<TConnectorOptions>, new()
{
  public virtual TConnectorOptions Options { get; init; } = new();

  public virtual string ConnectionId { get; } = null!;

  public virtual ConnectorStatus Status { get; protected set; }

  public virtual bool IsInit => Status == ConnectorStatus.Init;
  public virtual bool IsConnected => Status == ConnectorStatus.Connected;

  public virtual ConnectedDocument Document { get; protected set; } = null!;

  /// <summary>
  /// Use this for now, for simple client/server POC test. Worry about mapping using <see cref="Connections"/> later.
  /// </summary>
  public IConnection? Connection { get; protected set; }

  public ConnectionDictionary<TConnection> Connections { get; } = new();

  // ConnectionDictionary<IConnection> IConnector.Connections => (ConnectionDictionary<IConnection>)Connections;//.Cast<KeyValuePair<string, IConnection>>();

  internal ConnectorBase() { }

  public ConnectorBase(ConnectedDocument document, TConnectorOptions? options = default)
  {
    Init(document, options);
  }

  public void Init(ConnectedDocument rootDocument, IConnectorOptions<TConnectorOptions>? connectorOptions = default)
  {
    Document = rootDocument;
    connectorOptions?.CopyTo(Options);
  }

  public abstract Task Connect();

  public abstract void Disconnect();

  public void Receive(string connectionId, byte[] data)
  {
    Console.WriteLine($"Receive(): IsConnected={IsConnected} Connections.Count={Connections.Count} Options={this}\n\tconnectionId={connectionId}\n\tdata.Length={data.Length}");
    if (Connections.TryGetValue(connectionId, out TConnection? connection))
    {
      if (IsConnected)
      {
        SyncProtocol.ReadUpdate(connection.Stream, Document, this);
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
    Console.WriteLine($"Send(): IsConnected={IsConnected} Connections.Count={Connections.Count} Options={this}\n\tconnectionId={connectionId}\n\tdata={SyncProtocol.EncodeBytes(data)}");
    if (Connections.TryGetValue(connectionId, out TConnection? connection))
    {
      if (IsConnected)
      {
        SyncProtocol.WriteUpdate(connection.Stream, data);
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
    Console.WriteLine($"Broadcast(): IsConnected={IsConnected} Connections.Count={Connections.Count} Options={this}\n\tdata={SyncProtocol.EncodeBytes(data)}");
    if (IsConnected)
    {
      Connections.AsParallel().ForAll<TConnection>(
        connection => SyncProtocol.WriteUpdate(connection.Stream, data));
    }
    Console.WriteLine($"Broadcast(): End");
  }

  public void Broadcast(Action<Stream> streamAction)
  {
    Console.WriteLine($"Broadcast(): streamAction={SyncProtocol.EncodeBytes(streamAction.Method?.GetMethodBody()?.GetILAsByteArray()!)}");
    Connections.AsParallel().ForAll<TConnection>(
      connection => streamAction.Invoke((connection.Stream)));
  }
}
