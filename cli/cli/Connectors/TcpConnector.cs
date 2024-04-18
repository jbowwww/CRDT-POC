using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace cli.Connectors;

public class TcpConnector : ConnectorBase<TcpConnectorOptions>
{
  private readonly object _syncObject = new();

  public const int ListenSocketAcceptQueueSize = 8;

  public const int ClientConnectDelay = 2000;

  public const int PostConnectDelay = 500;
  // private readonly object _syncObject = new();

  public override string Id => Options.Endpoint.ToString();

  public override async Task Connect()
  {
    lock (_syncObject)
    {
      if (!IsInit)
      {
        Console.WriteLine($"TcpConnector.Connect(): Aborting on connector.Id={Id} because Status={Status}");
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
    Console.Write($"Disconnect(): Disconnecting client from connector.Id={Id} ... ");
    lock (_syncObject)
    {
      if (IsConnected)
      {
        Status = ConnectionStatus.Disconnecting;
      }
    }
    Console.WriteLine("OK");//$"Disconnect(): End Disconnect() client with connector.Id={Id} ...");
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
        Console.Write($"ServerListen(): Accepting connection from [{acceptedSocket.RemoteEndPoint}->{acceptedSocket.LocalEndPoint}] ... ");
        // Creates a TcpConnection object, which wraps the accepted and Task.Run()'s ConnectionBase.ServerMessageLoop
        var connection = new Connection(this, acceptedSocket, true);
        Console.WriteLine($"OK");//ServerListen(): Established connection={connection}");
        // Task WriteSyncStep1 then loops on ReadSyncMessage (client just sends updates when they happen)
        _ = Task.Run(() => connection.MessageLoop());
      }
      // lock (_syncObject)
      // {
      Console.Write($"ServerListen(): Closing the listener on Endpoint={Options.Endpoint} ...");
      listenSocket.Close(1000);
      while (Connections.Count > 0)
        ;
      if (Status == ConnectionStatus.Disconnecting)
      {
        Status = ConnectionStatus.Disconnected;
      }
      Console.Write("OK");
      // }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"ServerListen(): Exception: Options.Listen={Options.Listen} Status={Status}\n\t{ex}");
      Status = ConnectionStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
    }
  }

  public IConnection ClientConnect(IPEndPoint remoteEndpoint)
  {
    var client = new TcpClient(remoteEndpoint.AddressFamily);
    Console.Write($"ClientConnect(): Connecting to {remoteEndpoint} ... ");
    client.Connect(remoteEndpoint);
    var connection = new Connection(this, client.Client);
    Console.WriteLine("OK");//$"ClientConnect(): Established connection={connection}");
    _ = Task.Run(() => connection.MessageLoop());
    return connection;
  }
}
