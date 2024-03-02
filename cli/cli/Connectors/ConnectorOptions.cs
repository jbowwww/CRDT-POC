using System;
using Aemo.Connectors;

namespace Aemo;

public abstract class ConnectorOptions<T> : IConnectorOptions<T>
  where T : ConnectorOptions<T>, new()
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
}
