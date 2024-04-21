using System;
using cli.Connectors;

namespace cli;

public interface IConnectorOptions<TConnector>
   where TConnector : ConnectorOptions<TConnector>, new()
{
  void Parse(string[] args);

  TConnector CopyTo(IConnectorOptions<TConnector> options);
}
