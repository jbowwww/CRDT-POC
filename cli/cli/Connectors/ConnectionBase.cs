using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Ycs;

namespace Aemo.Connectors;

public abstract class ConnectionBase : IConnection, IDisposable
{
  public IConnector Connector { get; init; } = null!;

  public virtual string ConnectionId => RemoteEndpoint?.ToString() ?? throw new InvalidDataException($"{this}");

  public EndPoint? LocalEndpoint => Socket?.LocalEndPoint;

  public EndPoint? RemoteEndpoint => Socket?.RemoteEndPoint;

  public virtual Socket Socket { get; init; } = null!;

  public virtual NetworkStream Stream { get; init; } = null!;

  public virtual ConnectionStatus Status => Connector != null ? Connector.Status : ConnectionStatus.Init;

  public string ToString(string? suffix = null) =>
    $"[{GetType().Name} Id={this.ConnectionId} Status={Status}]: " +
    (suffix ?? $"LocalEndpoint={LocalEndpoint} RemoteEndpoint={RemoteEndpoint}");

  public override string ToString() => ToString(null);

  ~ConnectionBase()
  {
    Dispose();
  }

  public void Dispose()
  {
    System.GC.SuppressFinalize(this);
    if (Stream != null)
    {
      Stream.Close();
      // Stream = null;
    }
  }

  internal void MessageLoop(bool server = false)
  {
    if (Connector == null)
    {
      throw new InvalidOperationException(ToString($"MessageLoop(server={server}): Connector == null"));
    }
    Console.WriteLine(ToString($"MessageLoop(server={server}): START"));
    if (Status <= ConnectionStatus.Partitioned)
    {
      if (server)
      {
        Connector.Connections.Add(this);
        Console.WriteLine(ToString($"MessageLoop(server={server}): WriteSyncStep1"));
        WriteSyncStep1();
      }
      while (Status <= ConnectionStatus.Partitioned)
      {
        var available = Socket.Available;
        if (available > 0)
        {
          Console.WriteLine(ToString($"MessageLoop(server={server})] ReadSyncMessage available={available}"));
          var messageType = ReadSyncMessage();
          Console.WriteLine(ToString($"MessageLoop(server={server})] ReadSyncMessage returned messageType={messageType}\n\tupdatedDoc={Connector?.Document.ToString(Connector?.Document.ValuesToString())}"));
        }
      }
      if (server)
      {
        if (Connector == null)
        {
          throw new InvalidOperationException(ToString($"MessageLoop(server={server}): Connector == null"));
        }
        Connector.Connections.Remove(this);
        Console.WriteLine(ToString($"MessageLoop(server={server}): Disposing ..."));
      }
      Dispose();
    }
    Console.WriteLine(ToString($"MessageLoop(server={server}): END"));
  }

  public void WriteSyncStep1() => SyncProtocol.WriteSyncStep1(Stream, Connector?.Document);
  public void WriteSyncStep2(byte[] stateVector) => SyncProtocol.WriteSyncStep2(Stream, Connector?.Document, stateVector);
  public void ReadSyncStep1() => SyncProtocol.ReadSyncStep1(Stream, Connector?.Document);
  public void ReadSyncStep2() => SyncProtocol.ReadSyncStep2(Stream, Connector?.Document, this);
  public void WriteUpdate(byte[] update) => SyncProtocol.WriteUpdate(Stream, update);
  public void ReadUpdate() => SyncProtocol.ReadUpdate(Stream, Connector?.Document, this);
  public uint ReadSyncMessage() => SyncProtocol.ReadSyncMessage(Stream, Connector?.Document, this);
}
