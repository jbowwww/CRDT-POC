using System;
using Aemo.Connectors;

namespace Aemo;

public abstract class ConnectorOptions<TConnector, TConnectorOptions> : IConnectorOptions<TConnector>
  where TConnector : IConnector<TConnector, TConnectorOptions>, new()
  where TConnectorOptions : IConnectorOptions<TConnector>
{
  public abstract IConnectorOptions<TConnector> Parse(string[] args);

  public static bool TryParse(string[] args, ref IConnectorOptions<TConnector> options)
  {
    if (args != null && args.Length > 0)
    {
      try
      {
        options.Parse(args);
        return true;
      }
      catch (ArgumentException /*ex*/)
      {
        // TODO: Log etc
      }
    }
    return false;
  }
}
