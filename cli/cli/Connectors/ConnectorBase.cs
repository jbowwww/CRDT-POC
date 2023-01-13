using System;
using System.Collections;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ycs;

namespace Aemo.Connectors;

public static class ByteArray
{
  public static string ToString(byte[] bytes) => string.Join(" ", bytes.Select((b, i) => b.ToString("{00:X}")));
}

public abstract class ConnectorBase<TConnection, TConnectorOptions> : IConnector<TConnection>, IConnector
  where TConnection : IConnection, new()
  where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
{
  public virtual TConnectorOptions Options { get; init; } = new();

  public virtual string ConnectionId { get; } = null!;

  public virtual bool IsConnected { get; protected set; }

  public virtual ConnectedDocument Document { get; init; }

  /// <summary>
  /// Use this for now, for simple client/server POC test. Worry about mapping using <see cref="Connections"/> later.
  /// </summary>
  public IConnection? Connection { get; protected set; }

  public ConnectionDictionary<TConnection> Connections { get; } = new();

  ConnectionDictionary<IConnection> IConnector.Connections => (ConnectionDictionary<IConnection>)Connections.Cast<KeyValuePair<string, IConnection>>();

  public ConnectorBase(ConnectedDocument document, TConnectorOptions options)
  {
    Document = document;
    options.CopyTo(Options);
  }

  public abstract void Connect();

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
    Console.WriteLine($"Send(): IsConnected={IsConnected} Connections.Count={Connections.Count} Options={this}\n\tconnectionId={connectionId}\n\tdata={ByteArray.ToString(data)}");
    if (Connections.TryGetValue(connectionId, out TConnection? connection))
    {
      if (IsConnected)
      {
        SyncProtocol.WriteUpdate(Connections[connectionId].Stream, data);
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
    Console.WriteLine($"Broadcast(): IsConnected={IsConnected} Connections.Count={Connections.Count} Options={this}\n\tdata={ByteArray.ToString(data)}");
    if (IsConnected)
    {
      Connections.AsParallel().ForAll<TConnection>(
        connection => SyncProtocol.WriteUpdate(connection.Stream, data));
    }
    Console.WriteLine($"Broadcast(): End");
  }

  public void Broadcast(Action<Stream> streamAction)
  {
    Console.WriteLine($"Broadcast(): streamAction={streamAction.Method.GetMethodBody()}");
    using (var stream = new MemoryStream())
    {
      streamAction(stream);
      var data = stream.ToArray();
      Broadcast(data);
      Console.WriteLine($"Broadcast(): End");
    }
  }

  protected static string EncodeBytes(byte[] arr) => Convert.ToBase64String(arr);
  protected static byte[] DecodeString(string str) => Convert.FromBase64String(str);
}
