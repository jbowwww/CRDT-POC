using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Aemo.Connectors;

public sealed class TcpConnection : Connection
{
  public EndPoint? LocalEndpoint => Socket?.LocalEndPoint;

  public EndPoint? RemoteEndpoint => Socket?.RemoteEndPoint;

  public Socket? Socket { get; init; }

  public TcpConnection(TcpConnector connector, Socket socket, bool server = false)
    : base(
      connector,
      socket.RemoteEndPoint?.ToString() ?? throw new InvalidOperationException($"NetworkConnection.ctor: connector={connector}"),
      new NetworkStream(socket, true),
      server)
  { }

  public async static Task<TcpConnection> AcceptIncomingAsync(TcpConnector connector, Socket listenSocket)
  {
    var acceptedSocket = await listenSocket.AcceptAsync();
    Console.Write($"TcpConnector.Server(): Accepting {acceptedSocket.RemoteEndPoint} -> {acceptedSocket.LocalEndPoint} ... ");
    var connection = new TcpConnection(connector, acceptedSocket, true);
    Console.WriteLine($"OK: connection={connection}");
    return connection;
  }
}
