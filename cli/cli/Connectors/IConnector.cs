using System;
using System.Collections.Concurrent;
using System.IO;
namespace Aemo.Connectors;

public interface IConnector<TConnection> : IConnector
  where TConnection : IConnection, new()
{
  new ConnectionDictionary<TConnection> Connections { get; }
}

public interface IConnector
{
  ConnectionDictionary<IConnection> Connections { get; }

  string ConnectionId { get; }

  bool IsConnected { get; }

  ConnectedDocument Document { get; }

  void Connect();

  void Disconnect();

  void Receive(string connectionId, byte[] data);

  void Send(string connectionId, byte[] data);

  void Broadcast(byte[] data);

  void Broadcast(Action<Stream> streamAction);
}