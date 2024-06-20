using System;
using System.Threading.Tasks;
using Ycs;
using cli.Connectors;

namespace cli;

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

  public static string ValuesToString(this YDoc document) => document.GetMap().ToString("") ?? string.Empty;
  public static string ToString(this YDoc document) => ToString(document, ValuesToString(document));
  public static string ToString(this YDoc document, string? suffix = null) =>
    $"[{document.GetType().Name} ClientId={document.ClientId} map.Count={document.GetMap().Count}]" + (suffix != null ? (suffix.Contains("\n") ? "\n\t" : " ") + suffix : "");

  public static string ToSummaryString(this YDoc document) => $"[YDoc ClientId={document.ClientId} map.Count={document.GetMap().Count}]";
  // public static string ToString(this YDoc document) => $"{document.ToSummaryString()}: {document.GetMap()}";      // $"[{doc.GetType()} {(doc is ConnectedDocument ? (doc as ConnectedDocument)!.Name : "YDoc")}]: {doc.GetMap()}";

  public static async Task<TConnector> Connect<TConnector, TConnectorOptions>(
    this YDoc document,
    Action<TConnectorOptions>? connectorOptionsConfiguration = null
  )
    where TConnector : IConnector<TConnectorOptions>, new()
    where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
  {
    return await document.Connect<TConnector, TConnectorOptions>(
      new TConnectorOptions() { Document = document },
      connectorOptionsConfiguration
    );
  }

  public static async Task<TConnector> Connect<TConnector, TConnectorOptions>(
    this YDoc document,
    TConnectorOptions? connectorOptions = default,
    Action<TConnectorOptions>? connectorOptionsConfiguration = null
  )
    where TConnector : IConnector<TConnectorOptions>, new()
    where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
  {
    connectorOptions ??= new TConnectorOptions() { Document = document };
    connectorOptionsConfiguration?.Invoke(connectorOptions);
    var connector = new TConnector() { Options = connectorOptions };
    await connector.Connect();

    document.UpdateV2 += (object? sender, (byte[] data, object origin, Transaction transaction) e) =>
    {//\n\ttransaction={e.transaction}
      Console.WriteLine($"{document.ToSummaryString()}.UpdateV2(): sender={sender} origin={e.origin} document.ClientId={document.ClientId}, map.Count={document.GetMap().Count}]\n\tconnector={connector}");
      if (e.data != null && e.data.Length > 0 && connector.IsConnected && sender != null && e.origin == sender)
      {
        connector.Broadcast(connection =>
        {
          // Console.WriteLine($"{document.ToSummaryString()}.UpdateV2: Broadcast: connection={connection}");
          connection.WriteUpdate(document.EncodeStateAsUpdateV2()); // e.data); // TODO: Or encode state vector/update using state vector , and broadcast that ??
        });
      }
    };
    return connector;
  }

  private static string EncodeBytes(byte[] arr) => Convert.ToBase64String(arr);
  private static byte[] DecodeString(string str) => Convert.FromBase64String(str);
}