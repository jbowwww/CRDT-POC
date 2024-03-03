using System;
using System.Threading.Tasks;
using Aemo;
using Aemo.Connectors;
using Ycs;

public static class YDocExtensions
{

  // public string Name
  // {
  //   get => Get<string>("Name") ?? $"Doc#{ClientId}";
  //   set => Set("Name", value);
  // }

  public static TValue Get<TValue>(this YDoc document, string key = "")
    where TValue : class => (TValue)document.GetMap().Get(key);

  public static void Set(this YDoc document, string key, object value) =>
      document.Transact((tr) => document.GetMap().Set(key, value), document, true);

  // public string ValuesToString() => GetMap().ToString() ?? string.Empty;
  // public override string ToString() => ToString();
  // public string ToString(string? suffix = null) =>
  //   $"[{GetType().Name} Name={Name} ClientId={ClientId} map.Count={GetMap().Count}]" + (suffix != null ? " " + suffix : "");

  public static string ToSummaryString(this YDoc document) => $"[YDoc ClientId={document.ClientId} map.Count={document.GetMap().Count}]";
  public static string ToString(this YDoc document) => $"{document.ToSummaryString()}: {document.GetMap()}";      // $"[{doc.GetType()} {(doc is ConnectedDocument ? (doc as ConnectedDocument)!.Name : "YDoc")}]: {doc.GetMap()}";

  public static async Task<TConnector> Connect<TConnector, TConnectorOptions>(
    this YDoc document,
    Action<TConnectorOptions>? connectorOptionsConfiguration = null
  )
    where TConnector : IConnector<TConnector>, new()
    where TConnectorOptions : IConnectorOptions<TConnector>, new()
  {
    return await document.Connect<TConnector, TConnectorOptions>(
      new TConnectorOptions(),
      connectorOptionsConfiguration
    );
  }

  public static async Task<TConnector> Connect<TConnector, TConnectorOptions>(
    this YDoc document,
    // TConnectorOptions? connectorOptions = default,
    TConnectorOptions? connectorOptions = default,
    Action<TConnectorOptions>? connectorOptionsConfiguration = null
  )
    where TConnector : IConnector<TConnector>, new()
    where TConnectorOptions : IConnectorOptions<TConnector>, new()
  {
    connectorOptions ??= new TConnectorOptions();
    connectorOptionsConfiguration?.Invoke(connectorOptions);
    var connector = new TConnector()
    {
      Options = connectorOptions
    };
    await connector.Connect();

    document.UpdateV2 += (object? sender, (byte[] data, object origin, Transaction transaction) e) =>
    {
      Console.WriteLine($"{document.ToSummaryString()}.UpdateV2():\n\tsender={sender}\n\te.data={SyncProtocol.EncodeBytes(e.data)}\n\torigin={e.origin}\n\ttransaction={e.transaction}\n\tdocument={document}\n\tconnector={connector}");
      if (e.data != null && e.data.Length > 0 && connector.IsConnected && sender != null && e.origin == sender)
      {
        connector.Broadcast(connection =>
        {
          Console.WriteLine($"{document.ToSummaryString()}.UpdateV2: Broadcast: connection={connection}");
          connection.WriteUpdate(document.EncodeStateAsUpdateV2()); // e.data); // TODO: Or encode state vector/update using state vector , and broadcast that ??
        });
      }
    };
    return connector;
  }
}