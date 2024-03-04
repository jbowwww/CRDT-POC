using System;
using System.Net;
using System.Net.Sockets;

namespace Aemo.Connectors;

public sealed class NetworkConnection : Connection, IDisposable
{
  public EndPoint? LocalEndpoint => Socket?.LocalEndPoint;

  public EndPoint? RemoteEndpoint => Socket?.RemoteEndPoint;

  public Socket? Socket { get; init; }

  public NetworkConnection(TcpConnector connector, Socket socket)
    : base(
      connector,
      socket.RemoteEndPoint?.ToString() ?? throw new InvalidOperationException($"NetworkConnection.ctor: connector={connector}"),
      new NetworkStream(socket, true)
    )
  { }
}
