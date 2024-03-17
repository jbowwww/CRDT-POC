using System;
using System.IO;
using System.Threading.Tasks;
using Ycs;

namespace Aemo.Connectors;

public abstract class Connection : IConnection, IDisposable
{
    public IConnector Connector { get; init; }

    public string Id { get; init; }

    public Stream Stream { get; init; }

    public bool IsServer { get; init; } = false;

    public virtual ConnectionStatus Status => Connector != null ? Connector.Status : ConnectionStatus.Init;

    public string ToString(string? suffix = null) => $"[{GetType().Name} Id={this.Id} Status={Status}]: {suffix}";
    public override string ToString() => ToString(null);

    public Connection(IConnector connector, string id, Stream stream, bool isServer = false)
    {
        Connector = connector;
        Id = id;
        Stream = stream;
        IsServer = isServer;
        // if (isServer)
        // {

        //     WriteSyncStep1();
        //     connector.ServerConnections.Add(this);
        // }
        // RunMessageLoop();
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

    internal Task RunMessageLoop() => Task.Run(() => MessageLoop());
    internal void MessageLoop()
    {
        Console.WriteLine(ToString($".MessageLoop(): {this}"));
        if (IsServer)
        {
            WriteSyncStep1();
        }
        while (Status <= ConnectionStatus.Partitioned)
        {
            if (Stream.Length > 0)
            {
                ReadSyncMessage();
            }
        }
        Dispose();
        Console.WriteLine(ToString($"MessageLoop(): END"));
    }

    public void WriteSyncStep1() => SyncProtocol.WriteSyncStep1(Stream, Connector?.Document);
    public void WriteSyncStep2(byte[] stateVector) => SyncProtocol.WriteSyncStep2(Stream, Connector?.Document, stateVector);
    public void ReadSyncStep1() => SyncProtocol.ReadSyncStep1(Stream, Connector?.Document);
    public void ReadSyncStep2() => SyncProtocol.ReadSyncStep2(Stream, Connector?.Document, this);
    public void WriteUpdate(byte[] update) => SyncProtocol.WriteUpdate(Stream, update);
    public void ReadUpdate() => SyncProtocol.ReadUpdate(Stream, Connector?.Document, this);
    public uint ReadSyncMessage() => SyncProtocol.ReadSyncMessage(Stream, Connector?.Document, this);
}
