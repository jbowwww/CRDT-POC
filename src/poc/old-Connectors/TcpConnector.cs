using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ycs;

namespace Aemo.Connectors;

public class TcpConnector : ConnectorBase<NetworkConnection, TcpConnectorOptions>
{
  private readonly object _syncObject = new();

  public const int ListenSocketAcceptQueueSize = 8;

  public const int ClientConnectDelay = 2000;

  public const int PostConnectDelay = 500;
  // private readonly object _syncObject = new();

  public override string ConnectionId => Options.Endpoint.ToString();

  public override async Task Connect()
  {
    lock (_syncObject)
    {
      if (!IsInit)
      {
        Console.WriteLine($"TcpConnector.Connect(): returning because Status={Status}");
        return;
      }
      Status = ConnectionStatus.Connecting;
    }
    ServerListen();
    await Task.Delay(ClientConnectDelay);
    lock (_syncObject)
    {
      foreach (var remoteEndpoint in Options.RemoteEndpoints)
      {
        ClientConnect(remoteEndpoint);
      }
      Status = ConnectionStatus.Connected;
    }
  }

  public override void Disconnect()
  {
    Console.WriteLine($"Disconnect(): Disconnecting client with connectionId={ConnectionId} ...");
    lock (_syncObject)
    {
      if (IsConnected)
      {
        Status = ConnectionStatus.Disconnecting;
      }
    }
    Console.WriteLine($"Disconnect(): End Disconnect() client with connectionId={ConnectionId} ...");
  }

  public async void ServerListen()
  {
    var listenSocket = new Socket(Options.Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    try
    {
      listenSocket.Bind(Options.Endpoint);
      listenSocket.Listen(ListenSocketAcceptQueueSize);
      Console.WriteLine($"ServerListen(): Listening on {listenSocket.LocalEndPoint} ListenSocketAcceptQueueSize={ListenSocketAcceptQueueSize}...");
      while (Options.Listen && IsConnected)
      {
        var acceptedSocket = await listenSocket.AcceptAsync();
        Console.WriteLine($"ServerListen(): Accepting connection from [{acceptedSocket.RemoteEndPoint}->{acceptedSocket.LocalEndPoint}]");
        // Creates a TcpConnection object, which wraps the accepted and Task.Run()'s ConnectionBase.ServerMessageLoop
        var connection = new NetworkConnection(this, acceptedSocket);
        Console.WriteLine($"ServerListen(): Established connection={connection}");
        // Task WriteSyncStep1 then loops on ReadSyncMessage (client just sends updates when they happen)
        _ = Task.Run(() => connection.MessageLoop(true));
      }
      // lock (_syncObject)
      // {
      Console.WriteLine($"ServerListen(): Closing the listener on Endpoint={Options.Endpoint}");
      listenSocket.Close(1000);
      while (Connections.Count > 0)
        ;
      if (Status == ConnectionStatus.Disconnecting)
      {
        Status = ConnectionStatus.Disconnected;
      }
      // }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"ServerListen(): Exception: Options.Listen={Options.Listen} Status={Status}\n\t{ex}");
      Status = ConnectionStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
    }
  }

  public NetworkConnection ClientConnect(IPEndPoint remoteEndpoint)
  {
    var client = new TcpClient(remoteEndpoint.AddressFamily);
    Console.WriteLine($"ClientConnect(): Connecting to {client.Client.LocalEndPoint}->{client.Client.RemoteEndPoint} ...");
    client.Connect(remoteEndpoint);
    var connection = new NetworkConnection(this, client.Client);
    Console.WriteLine($"ClientConnect(): Established connection={connection}");
    _ = Task.Run(() => connection.MessageLoop(false));
    return connection;
  }
}
