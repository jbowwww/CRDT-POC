using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ycs;

namespace cli.Connectors;

public abstract class Connector<TConnectorOptions> : IConnector<TConnectorOptions>
    where TConnectorOptions : ConnectorOptions, new()
{
    public abstract string Id { get; }
    public virtual TConnectorOptions Options { get; init; } = null!;
    ConnectorOptions IConnector.Options { get => Options; init => Options = (TConnectorOptions)value; }
    public virtual YDoc Document => Options.Document;
    public ConnectionDictionary<IConnection> Connections { get; } = new();
    public ConnectionStatus Status { get; set; }
    public virtual bool IsInit => Status == ConnectionStatus.Init;
    public virtual bool IsConnected => Status <= ConnectionStatus.Partitioned;
    public virtual bool IsPartitioned => Status == ConnectionStatus.Partitioned;
    public virtual bool IsError => Status == ConnectionStatus.Error;
    public virtual bool IsDisconnected => Status >= ConnectionStatus.Disconnecting;

    public override string ToString() => $"[{GetType().Name} Id=\"{Id}\" Status={Status} Document.ClientId={Document?.ClientId} Connections={Connections}]";

    public Connector()
    {
        Options = new TConnectorOptions();
    }

    ~Connector()
    {
        Dispose();
    }

    public void Dispose()
    {
        Disconnect();
        System.GC.SuppressFinalize(this);
    }

    public void HandleClientConnected(IConnection connection) => Connections.Add(connection);

    public void HandleClientDisconnected(IConnection connection) => Connections.Remove(connection);

    public async Task EnqueueAndProcessMessagesAsync(string connectionId, long clock, MessageToProcess messageToEnqueue, CancellationToken cancellationToken = default)
    {

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
                connection.ReadUpdate();
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
        Console.WriteLine($"Send(): Status={Status} Connections.Count={Connections.Count} Options={this}\n\tconnectionId={connectionId}\n\tdata={EncodeBytes(data)}");
        if (Connections.TryGetValue(connectionId, out IConnection? connection))
        {
            if (IsConnected)
            {
                connection.WriteUpdate(data);
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
        Console.WriteLine($"Broadcast(): Status={Status} Connections.Count={Connections.Count} Options={this}\n\tdata={EncodeBytes(data)}");
        if (IsConnected)
        {
            Connections?.AsParallel<IConnection>().ForAll(
              connection => connection.WriteUpdate(data));
        }
        Console.WriteLine($"Broadcast(): End");
    }

    public void Broadcast(Action<IConnection> streamAction)
    {
        Connections.AsParallel<IConnection>().ForAll(streamAction.Invoke);
    }
    private static string EncodeBytes(byte[] arr) => Convert.ToBase64String(arr);
    private static byte[] DecodeString(string str) => Convert.FromBase64String(str);
}
