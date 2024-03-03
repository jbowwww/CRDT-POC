using System;
using Aemo.Connectors;

namespace Aemo;

public abstract class ConnectorOptions<TConnector> : IConnectorOptions
  where TConnector : IConnector, new()
{
  public string RootDocumentName { get; init; } = "ConnectedDocument";

  public abstract void Parse(string[] args);

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

  // public static IConnectorOptions<TConnector>? TryParse<TConnector>(string[] args)
  //   where TConnector : IConnector<TConnector>, new()
  // {
  //   IConnectorOptions<TConnector> options = new Connector();
  //   return TryParse<TConnector>(args, ref options) ? options : default;
  // }

  // public abstract TConnector CopyTo<TConnector>(IConnectorOptions<TConnector> options)
  //   where TConnector : ConnectorBase<TConnector>, new();

}

// public abstract class ConnectorOptions : IConnectorOptions
// {
// }
