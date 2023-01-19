using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ycs;

namespace Aemo.Connectors;

public class TcpConnector : ConnectorBase<TcpConnection, TcpConnectorOptions>
{
  public const int ListenSocketAcceptQueueSize = 8;

  public const int ClientConnectDelay = 2000;

  public const int PostConnectDelay = 500;
  private readonly object _syncObject = new();

  public override string ConnectionId => Options.Endpoint.ToString();

  public TcpConnector() : base()
  {

  }

  public TcpConnector(TcpConnectorOptions? options = default)
    : base(
      new ConnectedDocument($"Document#{Guid.NewGuid().ToString().Substring(0, 6)}"),
      options ?? new TcpConnectorOptions())
  { }

  public TcpConnector(ConnectedDocument document, TcpConnectorOptions? options = default)
   : base(document, options ?? new TcpConnectorOptions())
  {
    // TODO: Take an IPv4 address in connection string & a port number & connect a TCP socket
    // Note: You need a listener/server and a client, how to model/handle that??
    // TODO: Write a (de)multiplexer IConnector class(es) that probably use a MemoryStream/BufferedStream
    // to write to multiple TcpConnectors. ALso opposite for reading.
    // TODO: Then you either need one global parent/root doc containing all shared docs
    // and then you can read from connector's rxsocket and always update that global Root
    // OR maintain a map of constructed YDoc/ConnectedDocument's and write the ID
    // I think the 1st option is best there, because the doc structure implements subdoc maps already so use it
    // But I read somewhere there may be situations that are suited to the 2nd option (where did i read..?)
  }

  // internal override static ConnectorBase<TcpConnection, TcpConnectorOptions> Create(ConnectedDocument document, TcpConnectorOptions? options = default)
  // {
  //   var rootDocumentName = options?.RootDocumentName ?? "Document";
  //   var rootDocument = document ?? new ConnectedDocument(
  //     rootDocumentName,
  //     new ConnectedDocument.Options() { Name = rootDocumentName });
  //   var connector = new TcpConnector(rootDocument, options);
  //   return connector;

  public override async Task Connect()
  {
    lock (_syncObject)
    {
      if (Status == ConnectorStatus.Connecting || Status == ConnectorStatus.Connected)
      {
        return;
      }
      else if (Status == ConnectorStatus.Error || Status == ConnectorStatus.Disconnected)
      {
        throw new InvalidOperationException($"TcpConnector.Connect(): Status={Status}");
      }
      Status = ConnectorStatus.Connecting;
    }
    ServerListen();
    await Task.Delay(ClientConnectDelay);
    lock (_syncObject)
    {
      foreach (var remoteEndpoint in Options.RemoteEndpoints)
      {
        Connections.Add(ClientConnect(remoteEndpoint));
      }
      Status = ConnectorStatus.Connected;
    }
    await Task.Delay(PostConnectDelay);
  }

  public override void Disconnect()
  {
    Console.WriteLine($"Disconnect(): Disconnecting client with connectionId={ConnectionId} ...");
    lock (_syncObject)
    {
      if (Status <= ConnectorStatus.Connected)
        Status = ConnectorStatus.Disconnecting;
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
      Console.WriteLine($"ServerListen(): Listening on {Options.Endpoint} ...");
      while (Options.Listen)
      {
        Status = ConnectorStatus.Connecting;
        var connection = TcpConnection.FromSocket(await listenSocket.AcceptAsync(), this);
        Console.WriteLine($"ServerListen(): Accepted connection from {connection.Socket.RemoteEndPoint}");
        Status = ConnectorStatus.Connected;
        _ = Task.Run(() =>
        {
          Console.WriteLine($"ServerListen(): Listening on {connection.Socket.LocalEndPoint} to connection {connection.ConnectionId}"); // on {connection.Socket.RemoteEndPoint} ...
          while (Options.Listen && (Status == ConnectorStatus.Connecting || Status == ConnectorStatus.Connected))
          {
            if (connection.Stream.Socket.Available > 0)
            {
              var available = connection.Stream.Socket.Available;
              var messageType = SyncProtocol.ReadSyncMessage(connection.Stream, connection.Stream, Document, this);
              Console.WriteLine($"ServerListen(): connection.Stream.Socket.Available={available} for connection {connection.ConnectionId}: SyncProtocol.ReadSyncMessage returned {messageType}");

            }
          }
          Console.WriteLine($"ServerListen(): Closing server connection from {connection.Socket.RemoteEndPoint} Options.Listen={Options.Listen} Status={Status}");
          listenSocket.Close(1000);
          Status = ConnectorStatus.Disconnected;
        });
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"ServerListen(): Exception: Options.Listen={Options.Listen} Status={Status}\n\t{ex}");
      Status = ConnectorStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
    }
    Console.WriteLine($"Closing the listener on Endpoint={Options.Endpoint}");
  }

  public TcpConnection ClientConnect(IPEndPoint remoteEndpoint)
  {
    Console.WriteLine($"ClientConnect(): remoteEndpoint={remoteEndpoint}");
    var client = new TcpClient(remoteEndpoint.AddressFamily);
    client.Connect(remoteEndpoint);
    var connection = TcpConnection.FromSocket(client.Client, this);
    try
    {
      Console.WriteLine($"ClientConnect(): Established connection to {connection.ConnectionId}");
      // _ = Task.Run(() =>
      // {
      //   Console.WriteLine($"ClientConnect(): Listening on {connection.Socket.LocalEndPoint} to connection {connection.ConnectionId}"); // on {connection.Socket.RemoteEndPoint} ...
      //   while (Status == ConnectorStatus.Connecting || Status == ConnectorStatus.Connected)
      //   {
      //     if (client.Client.Available > 0)
      //     {
      //       var available = client.Client.Available;
      //       var messageType = SyncProtocol.ReadSyncMessage(connection.Stream, connection.Stream, Document, this);
      //       Console.WriteLine($"ClientConnect(): connection.Stream.Socket.Available={available} for connection {connection.ConnectionId}: SyncProtocol.ReadSyncMessage returned {messageType}");
      //     }
      //   }
      //   client.Close();
      //   Status = ConnectorStatus.Disconnected;
      //   Console.WriteLine($"ClientConnect(): Closing connection to server {connection.ConnectionId} Options.Listen={Options.Listen} Status={Status}");
      // });
    }
    catch (Exception ex)
    {
      Console.WriteLine($"ClientConnect(): Exception: Options.Listen={Options.Listen} Status={Status}\n\t{ex}");
      Status = ConnectorStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
    }
    Console.WriteLine($"ClientConnect(): Returning remoteEndpoint={remoteEndpoint}");
    return connection;
  }


}
