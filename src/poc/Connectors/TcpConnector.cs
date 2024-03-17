using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ycs;

namespace Aemo.Connectors;

public class TcpConnector : Connector<TcpConnector, TcpConnectorOptions>
{
  private readonly object _syncObject = new();

  public override string Id => Options.ListenEndpoint?.ToString() ?? "";

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
    _ = ServerListenIncomingAsync();
    await Task.Delay(Options.ClientConnectDelay);
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
    Console.WriteLine($"Disconnect(): Disconnecting client with connectionId={Id} ...");
    lock (_syncObject)
    {
      if (IsConnected)
      {
        Status = ConnectionStatus.Disconnecting;
      }
    }
    Console.WriteLine($"Disconnect(): End Disconnect() client with connectionId={Id} ...");
  }

  public async Task ServerListenIncomingAsync()
  {
    if (Options.ListenEndpoint == null)
      throw new ArgumentNullException("Options.ListenEndpoint");
    var listenSocket = new Socket(Options.ListenEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    try
    {
      Console.Write($"TcpConnector.Server(): Start listening on {listenSocket.LocalEndPoint} ListenSocketAcceptQueueSize={Options.ListenSocketAcceptQueueSize} ... ");
      listenSocket.Bind(Options.ListenEndpoint);
      listenSocket.Listen(Options.ListenSocketAcceptQueueSize);
      Console.WriteLine("OK.");
      while (Options.Listen && IsConnected)
      {
        var connection = await TcpConnection.AcceptIncomingAsync(this, listenSocket);
        Console.WriteLine($"OK: connection={connection}");
      }
      Console.WriteLine($"ServerListen(): Closing the listener on {Options.ListenEndpoint}");
      listenSocket.Close(1000);
      while (ServerConnections.Count > 0)
        ;
      if (Status == ConnectionStatus.Disconnecting)
      {
        Status = ConnectionStatus.Disconnected;
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"ServerListen(): Exception: Options.Listen={Options.Listen} Status={Status}\n\t{ex}");
      Status = ConnectionStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
    }
  }

  public TcpConnection ClientConnect(IPEndPoint remoteEndpoint)
  {
    var client = new TcpClient(remoteEndpoint.AddressFamily);
    Console.Write($"ClientConnect(): Connecting {client.Client.LocalEndPoint} -> {client.Client.RemoteEndPoint} ... ");
    try
    {
      client.Connect(remoteEndpoint);
      var connection = new TcpConnection(this, client.Client);
      Console.WriteLine($"OK: connection={connection}");
      return connection;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error! ex={ex}");
      throw;
    }
  }
}
