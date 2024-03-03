using System;
using System.Threading.Tasks;
using cli.Connectors;

namespace Aemo.Connectors;

public interface IConnector<TConnection, TConnectorOptions> : IConnector
  where TConnection : IConnection
  where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
{
  TConnectorOptions Options { get; init; }
}

public interface IConnector : IDisposable
{
  string ConnectionId { get; }
  ConnectionDictionary<IConnection> Connections { get; }

  ConnectionStatus Status { get; protected set; }

  bool IsConnected { get; }

  ConnectedDocument Document { get; }

  Task Connect();

  void Disconnect();

  void Receive(string connectionId, byte[] data);

  void Send(string connectionId, byte[] data);

  void Broadcast(byte[] data);

  void Broadcast(Action<IConnection> streamAction);
}