using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Linq;

namespace cli.Connectors;

public class TcpConnector : Connector<TcpConnectorOptions>
{
  private readonly object _syncObject = new();

  public const int ListenSocketAcceptQueueSize = 8;

  public const int ClientConnectDelay = 5000;

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
    // lock (_syncObject)
    // {
      foreach (var remoteEndpoint in Options.RemoteEndpoints)
      {
        await ClientConnect(remoteEndpoint);
      }
      Status = ConnectionStatus.Connected;
    // }
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
      await Task.Run(async () =>
      {
        while (Options.Listen && IsConnected)
        {
          var acceptedSocket = await listenSocket.AcceptAsync();
          Console.Write($"ServerListen(): Accepting connection from [{acceptedSocket.RemoteEndPoint}->{acceptedSocket.LocalEndPoint}] ... ");
          lock (_syncObject)
          {
            if (acceptedSocket.RemoteEndPoint is IPEndPoint remoteIP)
            {
              var remoteIdBuffer = new byte[256];
              var byteCount = acceptedSocket.Receive(remoteIdBuffer);
              var length = remoteIdBuffer[0];
              var remoteId = Encoding.ASCII.GetString(remoteIdBuffer, 1, byteCount - 1);
              if (Connections.ContainsKey(remoteId))
              {
                Console.Write($"Closing: Connection {remoteId} (byteCount={byteCount}) already exists: Connection=${Connections[remoteId]}");
                acceptedSocket.Shutdown(SocketShutdown.Both);
                acceptedSocket.Close();
                Console.WriteLine("OK");
              }
              else
              {
                Console.WriteLine($"OK (byteCount={byteCount} length={length} remoteId={remoteId})");
                var connection = new Connection(remoteId, this, acceptedSocket, true);
              }
            }
          }
        }
        lock (_syncObject)
        {
          Console.Write($"ServerListen(): Closing the listener on Endpoint={Options.Endpoint} ...");
          listenSocket.Close(1000);
          while (Connections.Count > 0)
            ;
          if (Status == ConnectionStatus.Disconnecting)
          {
            Status = ConnectionStatus.Disconnected;
          }
          Console.Write("OK");
        }
      });
    }
    catch (Exception ex)
    {
      Console.WriteLine($"ServerListen(): Exception: Options.Listen={Options.Listen} Status={Status}\n\t{ex}");
      Status = ConnectionStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
    }
  }

  public async Task<IConnection> ClientConnect(IPEndPoint remoteEndpoint)
  {
    if (!Connections.ContainsKey(remoteEndpoint.ToString()))
    {
      var client = new TcpClient(remoteEndpoint.AddressFamily);
      Console.Write($"ClientConnect(): Connecting to {remoteEndpoint} ... ");
      client.Connect(remoteEndpoint);
      Console.WriteLine($"OK");
      var connection = new Connection(remoteEndpoint.ToString(), this, client.Client);
      byte[] thisIdBuffer = Encoding.ASCII.GetBytes(Id).Prepend((byte)Id.Length).ToArray();
      //(byte[])new byte[1] { (byte)Id.Length }.Concat();
      var byteCount = client.Client.Send(thisIdBuffer);
      Console.WriteLine($"(Id={Id} thisIdBuffer={thisIdBuffer} byteCount={byteCount})");//$"ClientConnect(): Established connection={connection}");
      return connection;
    }
    else
    {
      var connection = Connections[remoteEndpoint.ToString()];
      Console.WriteLine($"ClientConnect(): Already have server connection to {remoteEndpoint}: {connection}");
      return connection;
    }
  }
}