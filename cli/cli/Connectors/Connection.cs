using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Ycs;

namespace cli.Connectors;

public class Connection : IConnection, IDisposable
{
    public IConnector Connector { get; init; } = null!;

    public string Id => RemoteEndpoint?.ToString() ?? throw new InvalidDataException($"{this}");

    public EndPoint? LocalEndpoint => Socket?.LocalEndPoint;

    public EndPoint? RemoteEndpoint => Socket?.RemoteEndPoint;

    public Socket Socket { get; init; } = null!;

    public bool IsServer { get; init; } = false;

    public Stream Stream { get; init; } = null!;

    public ConnectionStatus Status => Connector != null ? Connector.Status : ConnectionStatus.Init;

    public string ToString(string? suffix = null) =>
      $"[{GetType().Name} Id={this.Id} Status={Status} IsServer={IsServer}]: " +
      (suffix);// ?? $"LocalEndpoint={LocalEndpoint} RemoteEndpoint={RemoteEndpoint}");

    public override string ToString() => ToString(null);

    public Connection(IConnector connector, Socket socket, bool isServer = false)
    {
        Connector = connector;
        IsServer = isServer;
        Socket = socket;
        Stream = new NetworkStream(socket, true);
    }

    ~Connection()
    {
        Dispose();
    }

    public void Dispose()
    {
        System.GC.SuppressFinalize(this);
        Stream?.Close();
    }

    internal void MessageLoop()
    {
        if (Connector == null)
        {
            throw new InvalidOperationException(ToString($"MessageLoop: Connector == null"));
        }
        Console.WriteLine(ToString($"MessageLoop: START"));
        if (Status <= ConnectionStatus.Partitioned)
        {
            if (IsServer)
            {
                Connector.Connections.Add(this);
                Console.WriteLine(ToString($"MessageLoop: WriteSyncStep1"));
                WriteSyncStep1();
            }
            while (Status <= ConnectionStatus.Partitioned)
            {
                var available = Socket.Available;
                if (available > 0)
                {
                    Console.WriteLine(ToString($"MessageLoop] ReadSyncMessage available={available}"));
                    var messageType = ReadSyncMessage();
                    Console.WriteLine(ToString($"MessageLoop] ReadSyncMessage returned messageType={messageType}\n\tupdatedDoc={Connector?.Document.ToString(Connector?.Document.ValuesToString())}"));
                }
            }
            if (IsServer)
            {
                if (Connector == null)
                {
                    throw new InvalidOperationException(ToString($"MessageLoop: Connector == null"));
                }
                Connector.Connections.Remove(this);
                Console.WriteLine(ToString($"MessageLoop: Disposing ..."));
            }
            Dispose();
        }
        Console.WriteLine(ToString($"MessageLoop: END"));
    }

    public void WriteSyncStep1() => SyncProtocol.WriteSyncStep1(Stream, Connector?.Document);
    public void WriteSyncStep2(byte[] stateVector) => SyncProtocol.WriteSyncStep2(Stream, Connector?.Document, stateVector);
    public void ReadSyncStep1() => SyncProtocol.ReadSyncStep1(Stream, Stream, Connector?.Document);
    public void ReadSyncStep2() => SyncProtocol.ReadSyncStep2(Stream, Connector?.Document, this);
    public void WriteUpdate(byte[] update) => SyncProtocol.WriteUpdate(Stream, update);
    public void ReadUpdate() => SyncProtocol.ReadUpdate(Stream, Connector?.Document, this);
    public uint ReadSyncMessage() => SyncProtocol.ReadSyncMessage(Stream, Stream, Connector?.Document, this);
}
