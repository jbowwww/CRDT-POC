using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Aemo.Connectors;

public interface IConnector<TConnection, TConnectorOptions> : IConnector
  where TConnection : ConnectionBase, IConnection, new()
  where TConnectorOptions : IConnectorOptions<TConnectorOptions>, new()
{
  TConnectorOptions Options { get; init; }

  ConnectionDictionary<TConnection> Connections { get; }

  internal void Init(ConnectedDocument rootDocument, IConnectorOptions<TConnectorOptions>? connectorOptions = default);
}

public interface IConnector
{
  string ConnectionId { get; }

  ConnectorStatus Status { get; protected set; }

  bool IsConnected { get; }

  ConnectedDocument Document { get; }

  Task Connect();

  void Disconnect();

  void Receive(string connectionId, byte[] data);

  void Send(string connectionId, byte[] data);

  void Broadcast(byte[] data);

  void Broadcast(Action<Stream> streamAction);
}