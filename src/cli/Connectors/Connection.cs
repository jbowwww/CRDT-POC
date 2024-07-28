using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading;

using Ycs;

namespace cli.Connectors;

public enum YjsCommandType
{
    GetMissing,
    Update
}

public class MessageToProcess
{
    public YjsCommandType Command;
    public YjsCommandType? InReplyTo;
    public required string Data;
}

internal class MessageToClient
{
    [JsonPropertyName("clock")]
    public long Clock { get; set; }

    [JsonPropertyName("data")]
    public required string Data { get; set; }

    [JsonPropertyName("inReplyTo")]
    public YjsCommandType? InReplyTo { get; set; }
}

public class Connection : IConnection, IDisposable
{
    public const uint MessageYjsSyncStep1 = 0;
    public const uint MessageYjsSyncStep2 = 1;
    public const uint MessageYjsUpdate = 2;

    public IConnector Connector { get; init; }

    public string Id { get; init; }

    public EndPoint LocalEndpoint => Socket.LocalEndPoint!;// ?? throw new InvalidDataException($"Connection: LocalEndpoint={LocalEndpoint} RemoteEndpoint={RemoteEndpoint}");

    public EndPoint RemoteEndpoint => Socket.RemoteEndPoint!;// ?? throw new InvalidDataException($"Connection: LocalEndpoint={LocalEndpoint} RemoteEndpoint={RemoteEndpoint}");

    public Socket Socket { get; init; } = null!;

    public bool IsServer { get; init; } = false;

    public Stream Stream { get; init; } = null!;

    public SortedList<long, MessageToProcess> Messages { get; } = new SortedList<long, MessageToProcess>();

public class VersionClock
{
    private long _synced = 0;
    private long _serverClock = -1;
    private long _clientClock = -1;

    public bool Synced
    {
        get => _synced != 0;
        set => Interlocked.Exchange(ref _synced, value ? 1 : 0);
    }

    public long ServerClock => _serverClock;
    public long ClientClock => _clientClock;

    public long IncrementAndGetServerClock() => Interlocked.Increment(ref _serverClock);
    public long IncrementAndGetClientClock() => Interlocked.Increment(ref _clientClock);
    public void ReassignClientClock(long clock) => Interlocked.Exchange(ref _clientClock, clock);    
}
    
    public VersionClock Clock = new ();

    public bool Synced
    {
        get => Clock.Synced;
        set => Clock.Synced = value;
    }

    public ConnectionStatus? ConnectionStatus { get; protected set; }
    public ConnectionStatus Status
    {
        get => ConnectionStatus ?? Connector.Status;
        set => ConnectionStatus = value;
    }

    public int BytesAvailable => Socket.Available;

    public bool IsDataAvailable => BytesAvailable > 0;

    public Exception? Error { get; protected set; }

    protected byte[]? StateVector { get; set; } = null;

    public string ToString(string? suffix = null) => $"[{GetType().Name} Id={this.Id} Status={Status} IsServer={IsServer}]{(suffix != null ? ": " + suffix : "")}";
    // ?? $"LocalEndpoint={LocalEndpoint} RemoteEndpoint={RemoteEndpoint}");

    public override string ToString() => ToString(null);

    public Connection(string id, IConnector connector, Socket socket, bool isServer = false)
    {
        Id = id;
        Connector = connector;
        IsServer = isServer;
        Socket = socket;
        Stream = new NetworkStream(socket, true);
        Connector.Connections.Add(this);
        if (IsServer)
        {
            WriteSyncStep1();
        }
        // _ = Task.Run(() => MessageLoop());
    }

    ~Connection()
    {
        Dispose();
    }

    public void Dispose()
    {
        Connector.Connections.Remove(this);
        System.GC.SuppressFinalize(this);
        Stream.Close();
    }

    public void Disconnect()
    {
        Status = Connectors.ConnectionStatus.Disconnecting;
    }
    
    public void MessageLoop()
    {
        // Console.WriteLine(ToString($"MessageLoop: START this={this}")); //IsServer={IsServer}"));
        // if (!IsServer)
        // {
        //     WriteSyncStep1();
        // }
        // while (Status <= cli.Connectors.ConnectionStatus.Partitioned && Connector.Status < cli.Connectors.ConnectionStatus.Disconnecting)
        // {
        while (Socket.Available > 0)
        {
            var messageType = ReadSyncMessage();
            Console.WriteLine(ToString($"MessageLoop: ReadSyncMessage available={Socket.Available} ... messageType={messageType}"));
        }
        // }
        // Console.WriteLine(ToString($"MessageLoop: END this={this}"));
        // Dispose();
    }

    public void WriteSyncStep1()
    {
        Stream.WriteVarUint(MessageYjsSyncStep1);
        var sv = Connector.Document.EncodeStateVectorV2();
        Stream.WriteVarUint8Array(sv);
    }

    public void WriteSyncStep2(byte[] encodedStateVector)
    {
        Stream.WriteVarUint(MessageYjsSyncStep2);
        var update = Connector.Document.EncodeStateAsUpdateV2(encodedStateVector);
        Stream.WriteVarUint8Array(update);
    }

    public void ReadSyncStep1()
    {
        StateVector = Stream.ReadVarUint8Array();
        WriteSyncStep2(StateVector);
    }

    public void ReadSyncStep2()
    {
        var update = Stream.ReadVarUint8Array();
        Connector.Document.ApplyUpdateV2(update, this);
        if (!Synced)
        {
            WriteSyncStep1();
            Synced = true;
            Console.WriteLine($"MessageLoop: Synced (full) (client) {this}");
        }
    }

    public void WriteUpdate(byte[] update)
    {
        if (Synced)
        {
            Stream.WriteVarUint(MessageYjsUpdate);
            Stream.WriteVarUint8Array(update);
        }
    }

    public void ReadUpdate()
    {
        // if (Synced)
        // {
            ReadSyncStep2();
        // }
    }

    public uint ReadSyncMessage()
    {
        var messageType = Stream.ReadVarUint();

        switch (messageType)
        {
            case MessageYjsSyncStep1:
                ReadSyncStep1();
                break;
            case MessageYjsSyncStep2:
                ReadSyncStep2();
                break;
            case MessageYjsUpdate:
                ReadUpdate();
                break;
            default:
                throw new Exception($"Unknown message type: {messageType}");
        }

        Console.WriteLine(ToString($"MessageLoop: ReadSyncMessage returned messageType={messageType}\n\tupdatedDoc={Connector.Document.ToString(Connector.Document.ValuesToString())}"));

        return messageType;
    }
}
