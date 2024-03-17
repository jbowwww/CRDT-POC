using System;
using System.Threading.Tasks;
using cli.Connectors;
using Ycs;

namespace Aemo.Connectors;

public interface IConnector<TConnector, TConnectorOptions> : IConnector
  where TConnector : IConnector
  where TConnectorOptions : IConnectorOptions<TConnector>
{
  TConnectorOptions Options { get; init; }
}

public interface IConnector : IDisposable
{
  string Id { get; }
  ConnectionDictionary<IConnection> ServerConnections { get; }

  ConnectionStatus Status { get; protected set; }

  bool IsConnected { get; }

  YDoc Document { get; init; }

  Task Connect();

  void Disconnect();

  void Receive(string connectionId, byte[] data);

  void Send(string connectionId, byte[] data);

  void Broadcast(byte[] data);

  void Broadcast(Action<IConnection> streamAction);
}