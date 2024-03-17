using System;
using System.Threading.Tasks;
using Aemo;
using Aemo.Connectors;
using Ycs;
using Poc;

namespace Poc;

public static class YDocExtensions
{
    public static string ToString(this YDoc document, bool includeDocument = true) =>
        $"[YDoc ClientId={document.ClientId} map.Count={document.GetMap().Count}]:"
        + (!includeDocument ? " " : "\n\t" + document.GetMap().ToString("\n\t"));

    public static TValue Get<TValue>(this YDoc document, string key = "")
      where TValue : class => (TValue)document.GetMap().Get(key);

    public static void Set(this YDoc document, string key, object value) =>
        document.Transact((tr) => document.GetMap().Set(key, value), document, true);

    public static async Task<TConnector> Connect<TConnector, TConnectorOptions>(this YDoc document, TConnectorOptions connectorOptions
    )
      where TConnector : IConnector<TConnector, TConnectorOptions>, new()
      where TConnectorOptions : IConnectorOptions<TConnector>
    {
        var connector = new TConnector()
        {
            Options = connectorOptions
        };
        await connector.Connect();

        document.UpdateV2 += HandleUpdate(document, connector);
        return connector;
    }

    private static EventHandler<(byte[] data, object origin, Transaction transaction)>
        HandleUpdate<TConnector, TConnectorOptions>
    (YDoc document, IConnector<TConnector, TConnectorOptions> connector)
        where TConnector : IConnector<TConnector, TConnectorOptions>, new()
        where TConnectorOptions : IConnectorOptions<TConnector>
    {
        return (sender, e) =>
        {
            Console.WriteLine($"{document.ToString(false)}.UpdateV2():\n\tsender={sender}\n\te.data={SyncProtocol.EncodeBytes(e.data)}\n\torigin={e.origin}\n\ttransaction={e.transaction}\n\tdocument={document}\n\tconnector={connector}");
            if (e.data != null && e.data.Length > 0 && connector.IsConnected && sender != null && e.origin == sender)
            {
                connector.Broadcast(connection =>
                {
                    Console.WriteLine($"{document.ToString(false)}.UpdateV2: Broadcast: connection={connection}");
                    connection.WriteUpdate(document.EncodeStateAsUpdateV2()); // e.data); // TODO: Or encode state vector/update using state vector , and broadcast that ??
                });
            }
        };
    }
}