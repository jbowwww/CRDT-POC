using System;
using Aemo.Connectors;

namespace Aemo;

public interface IConnectorOptions<TConnector>
   where TConnector : ConnectorOptions<TConnector>, new()
{
  void Parse(string[] args);

  TConnector CopyTo(IConnectorOptions<TConnector> options);
}
