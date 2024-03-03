using System;
using Aemo.Connectors;

namespace Aemo;

public interface IConnectorOptions<T>
   where T : ConnectorOptions<T>, new()
{
  void Parse(string[] args);

  T CopyTo(IConnectorOptions<T> options);

  TConnector CreateConnector<TConnector, TConnection>(ConnectedDocument document)
    where TConnector : ConnectorBase<TConnection, T>, new()
    where TConnection : ConnectionBase;
}
