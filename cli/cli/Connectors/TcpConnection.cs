using System;
using System.IO;
using System.Net.Sockets;

namespace Aemo.Connectors;

public class TcpConnection : ConnectionBase
{
  public static TcpConnection FromSocket(Socket socket, TcpConnector connector)
  {
    return new TcpConnection()
    {
      Connector = connector,
      ConnectionId = socket.RemoteEndPoint?.ToString() ?? throw new InvalidOperationException($"receivingSocket.RemoteEndPoint null"),
      Socket = socket,
      Stream = new NetworkStream(socket, true)
    };
  }
}
