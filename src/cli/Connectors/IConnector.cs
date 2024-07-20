using System;
using System.Threading.Tasks;

using Ycs;

namespace cli.Connectors;

public interface IConnector<TConnectorOptions> : IConnector
  where TConnectorOptions : ConnectorOptions, new()
{
    new TConnectorOptions Options { get; init; }
}

public interface IConnector : IDisposable
{
    ConnectorOptions Options { get; init; }
    string Id { get; }
    ConnectionDictionary<IConnection> Connections { get; }
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