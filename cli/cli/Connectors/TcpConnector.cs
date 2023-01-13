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

  public override string ConnectionId => Options.Endpoint.ToString();

  public override bool IsConnected { get; protected set; }

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

  public override async void Connect()
  {
    ServerListen();
    await Task.Delay(ClientConnectDelay);
    foreach (var remoteEndpoint in Options.RemoteEndpoints)
    {
      Connections.Add(ClientConnect(remoteEndpoint));
    }
    IsConnected = true;
    await Task.Delay(PostConnectDelay);
    Document.AddUpdateHandler(HandleDocumentUpdate);
  }

  public override void Disconnect()
  {
    IsConnected = false;
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
        var connection = TcpConnection.FromSocket(await listenSocket.AcceptAsync(), this);
        Console.WriteLine($"ServerListen(): Accepted connection {connection.ConnectionId}");
        _ = Task.Run(() =>
        {
          Console.WriteLine($"ServerListen(): Listening to connection {connection.ConnectionId} ...");
          while (Options.Listen)
          {
            if (connection.Stream.Socket.Available > 0)
            {
              SyncProtocol.ReadSyncMessage(connection.Stream, connection.Stream, Document, this);
            }
          }
          Console.WriteLine($"ServerListen(): Closing server connection {connection.ConnectionId}");
        });
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
    }
    Console.WriteLine($"Closing the listener on Endpoint={Options.Endpoint}");
  }

  public TcpConnection ClientConnect(IPEndPoint remoteEndpoint)
  {
    Console.WriteLine($"ClientConnect(): remoteEndpoint={remoteEndpoint}");
    var client = new TcpClient(remoteEndpoint.AddressFamily);
    client.Connect(remoteEndpoint);
    var connection = TcpConnection.FromSocket(client.Client, this);
    _ = Task.Run(() =>
    {
      while (IsConnected)
      {
        if (client.Client.Available > 0)
        {
          var networkStream = client.GetStream();
          SyncProtocol.ReadSyncMessage(networkStream, networkStream, Document, this);
        }
      }

    });
    return connection;
  }

  public void HandleDocumentUpdate
    (object? sender, (byte[] data, object origin, Transaction transaction) e)
    {
    Console.WriteLine($"TcpConnector.HandleDocumentUpdate() sender={sender} e.data={e.data} origin={e.origin} transaction={e.transaction}");
    if (e.data == null || e.data.Length == 0)
    {
      return;
    }
    Broadcast(e.data);
    }
}
