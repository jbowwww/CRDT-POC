using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ycs;

namespace cli.Connectors;

public abstract class Connection : IConnection, IDisposable
{
    public const uint MessageYjsSyncStep1 = 0;
    public const uint MessageYjsSyncStep2 = 1;
    public const uint MessageYjsUpdate = 2;

    public IConnector Connector { get; init; }

    public abstract string Id { get; init; }

    public EndPoint LocalEndpoint => Socket.LocalEndPoint!;// ?? throw new InvalidDataException($"Connection: LocalEndpoint={LocalEndpoint} RemoteEndpoint={RemoteEndpoint}");

    public EndPoint RemoteEndpoint => Socket.RemoteEndPoint!;// ?? throw new InvalidDataException($"Connection: LocalEndpoint={LocalEndpoint} RemoteEndpoint={RemoteEndpoint}");

    public Socket Socket { get; protected set; } = null!;

    public bool IsServer { get; init; } = false;

    public Stream Stream { get; protected set; } = null!;

    public bool Synced { get; protected set; } = false;

    public ConnectionStatus? ConnectionStatus { get; protected set; }
    public ConnectionStatus Status
    {
        get => ConnectionStatus ?? Connector.Status;
        set => ConnectionStatus = value;
    }

    public Exception? Error { get; protected set; }

    protected byte[]? StateVector { get; set; } = null;

    public string ToString(string? suffix = null) =>
        $"[{GetType().Name} Id={this.Id} Status={Status} IsServer={IsServer}]: {suffix}";// ?? $"LocalEndpoint={LocalEndpoint} RemoteEndpoint={RemoteEndpoint}");

    public override string ToString() => ToString(null);

    public Connection(IConnector connector)
    {
        Connector = connector;
        StartMessageLoop();
    }

    protected void Connect(Socket socket)
    {
        Socket = socket;
        Stream = new NetworkStream(Socket, true);
        //         _ = Task.Run(() => MessageLoop());
    }

    //     private Connection(string id, IConnector connector, Socket socket, bool isServer = false)
    //     {
    //         Id = id;
    //         Connector = connector;
    //         IsServer = isServer;
    //         Socket = socket;
    //         Console.Write($"Connection(): Id={Id} Connector.Id={Connector.Id} IsServer={IsServer} ");
    //         if (IsServer)
    //         {
    //             var remoteIdBuffer = new byte[256];
    //             var byteCount = Socket.Receive(remoteIdBuffer);
    //             var length = remoteIdBuffer[0];
    //             var remoteId = Encoding.ASCII.GetString(remoteIdBuffer, 1, byteCount - 1);
    //             Console.Write($"remoteId={remoteId} "); 
    //             if (Connector.Connections.ContainsKey(remoteId))
    //             {
    //                 Console.Write($"Closing: Connection {remoteId} (byteCount={byteCount}) already exists: Connection=${Connections[remoteId]}");
    // Socket.Shutdown(SocketShutdown.Both);
    //                 Socket.Close();
    //                 Console.WriteLine("OK");
    //             }
    //             else
    //             {
    //                 Console.WriteLine($"OK (byteCount={byteCount} length={length} remoteId={remoteId})");
    //                 var connection = new Connection(remoteId, this, Socket, true);
    //             }
    // IsServer ?  $"Accepting connection from [{Socket.RemoteEndPoint}->{Socket.LocalEndPoint}] ... ")
    //         :   $"Connecting to "
    // var remoteIdBuffer = new byte[256];
    //         var byteCount = Socket.Receive(remoteIdBuffer);
    //         var length = remoteIdBuffer[0];
    //         var remoteId = Encoding.ASCII.GetString(remoteIdBuffer, 1, byteCount - 1);
    //         if (connector.Connections.ContainsKey(remoteId))
    //         {
    //             Console.Write($"Closing: Connection {remoteId} (byteCount={byteCount}) already exists: Connection=${Connections[remoteId]}");
    //             Socket.Shutdown(SocketShutdown.Both);
    //             Socket.Close();
    //             Console.WriteLine("OK");
    //         }
    //         else
    //         {
    //             Console.WriteLine($"OK (byteCount={byteCount} length={length} remoteId={remoteId})");
    //             var connection = new Connection(remoteId, this, Socket, true);
    //         }
    //         Stream = new NetworkStream(socket, true);
    //         Connector.Connections.Add(this);
    //         _ = Task.Run(() => MessageLoop());
    //     }

    ~Connection()
    {
        Dispose();
    }

    public void Dispose()
    {
        System.GC.SuppressFinalize(this);
        Socket.Shutdown(SocketShutdown.Both);
        Stream.Close();
    }

    public void Disconnect()
    {
        Status = Connectors.ConnectionStatus.Disconnecting;
    }
    
    internal void StartMessageLoop()
    {
        _ = Task.Run(() => MessageLoop());
    }
    
    internal void MessageLoop()
    {
        Console.WriteLine(ToString($"MessageLoop: START this={this}")); //IsServer={IsServer}"));
        if (!IsServer)
        {
            WriteSyncStep1();
        }
        while (Status <= cli.Connectors.ConnectionStatus.Partitioned && Connector.Status < cli.Connectors.ConnectionStatus.Disconnecting)
        {
            var available = Socket.Available;
            if (available > 0)
            {
                Console.WriteLine(ToString($"MessageLoop: ReadSyncMessage available={available}"));
                var messageType = ReadSyncMessage();
            }
        }
        Console.WriteLine(ToString($"MessageLoop: END this={this}"));
        Dispose();
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
        if (!Synced && IsServer)
        {
            Console.WriteLine($"MessageLoop: Synced (half) (server) {this}");
            WriteSyncStep1();
            Synced = true;
        }
    }

    public void ReadSyncStep2()
    {
        var update = Stream.ReadVarUint8Array();
        Connector.Document.ApplyUpdateV2(update, this);
        if (!Synced && !IsServer)
        {
            Console.WriteLine($"MessageLoop: Synced (full) (client) {this}");
            Synced = true;
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
        ReadSyncStep2();
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
