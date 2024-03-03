using Aemo.Connectors;

namespace Aemo;

public interface IConnectorOptions
{
  void Parse(string[] args);
}

public interface IConnectorOptions<TConnector> : IConnectorOptions
  where TConnector : IConnector<TConnector>, new()
{
  // TConnector CopyTo(IConnectorOptions<TConnector> options);
}
