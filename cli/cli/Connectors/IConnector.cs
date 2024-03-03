using System;
using System.Threading.Tasks;
using cli.Connectors;
using Ycs;

namespace Aemo.Connectors;

public interface IConnector<TConnector> : IConnector
  where TConnector : IConnector, new()
{
  IConnectorOptions<TConnector> Options { get; init; }
}

public interface IConnector : IDisposable
{
  string Id { get; }
  ConnectionDictionary Connections { get; }
  ConnectionStatus Status { get; protected set; }
  bool IsConnected { get; }
  YDoc Document { get; }
  Task Connect();
  void Disconnect();
  void Receive(string connectionId, byte[] data);
  void Send(string connectionId, byte[] data);
  void Broadcast(byte[] data);
  void Broadcast(Action<IConnection> streamAction);
}