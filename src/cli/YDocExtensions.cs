using System;
using System.Threading.Tasks;
using Ycs;
using cli.Connectors;

namespace cli;

public static class YDocExtensions
{
    public static TValue Get<TValue>(this YDoc document, string key = "") where TValue : class => (TValue)document.GetMap().Get(key);

    public static void Set(this YDoc document, string key, object value) => document.Transact((tr) => document.GetMap().Set(key, value), document, true);

    public static string ValuesToString(this YDoc document) => document.GetMap().ToString("") ?? string.Empty;
    public static string ToString(this YDoc document) => ToString(document, ValuesToString(document));
    public static string ToString(this YDoc document, string? suffix = null) =>
      $"[{document.GetType().Name} ClientId={document.ClientId} map.Count={document.GetMap().Count}]" +
      (suffix != null ? (suffix.Contains("\n") ? "\n\t" : " ") + suffix : "");
    public static string ToSummaryString(this YDoc document) => $"[YDoc ClientId={document.ClientId} map.Count={document.GetMap().Count}]";

    public static async Task<TConnector> Connect<TConnector>(this YDoc document, ConnectorOptions? connectorOptions = default)
      where TConnector : IConnector, new()
    {
        connectorOptions ??= new ConnectorOptions();
        connectorOptions.Document = document;
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
                connection.WriteUpdate(e.data); // document.EncodeStateAsUpdateV2()); // TODO: Or encode state vector/update using state vector , and broadcast that ??
            });
            }
        };
        return connector;
    }

    private static string EncodeBytes(byte[] arr) => Convert.ToBase64String(arr);
    private static byte[] DecodeString(string str) => Convert.FromBase64String(str);
}