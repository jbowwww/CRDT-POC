using System;
using System.Threading.Tasks;
using Aemo.Connectors;

namespace Aemo;

public abstract class ConnectorOptions<T> : IConnectorOptions<T>
  where T : IConnectorOptions<T>, new()
{
  public string RootDocumentName { get; init; } = "ConnectedDocument";

  public abstract void Parse(string[] args);

  public static bool TryParse(string[] args, ref T options)
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

  public static T? TryParse(string[] args)
  {
    T options = new();
    return TryParse(args, ref options) ? options : default;
  }

  public abstract T CopyTo(IConnectorOptions<T> options);

  public TConnector CreateConnector<TConnector, TConnection>(ConnectedDocument document = null)
    where TConnector : ConnectorBase<TConnection, T>, new()
    where TConnection : ConnectionBase, IConnection, new()
  {
    var rootDocumentName = RootDocumentName ?? "Document";
    var rootDocument = document ?? new ConnectedDocument(
      rootDocumentName,
      new ConnectedDocument.Options() { Name = rootDocumentName });
    var connector = new TConnector();
    connector.Init(rootDocument, this);
    return connector;
  }
}
