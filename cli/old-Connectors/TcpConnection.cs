using System;
using System.Net.Sockets;

namespace Aemo.Connectors;

public sealed class TcpConnection : ConnectionBase, IDisposable
{
  public TcpConnection(TcpConnector connector, Socket socket)
  {
    Connector = connector;
    Socket = socket;
    Stream = new NetworkStream(socket, true);
  }
}
