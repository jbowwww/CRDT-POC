using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aemo.Connectors;
using Ycs;

namespace Aemo;

public class ConnectedDocument : YDoc, IDisposable
{
  public string Name
  {
    get => Get<string>("Name") ?? $"Doc#{ClientId}";
    set => Set("Name", value);
  }

  public IConnector? Connector { get; protected set; }

  public ConnectedDocument(YDocOptions? options = null)
   : base(options)
  {
    UpdateV2 += HandleDocumentUpdate;
  }

  void IDisposable.Dispose()
  {
    System.GC.SuppressFinalize(this);
    if (Connector != null && Connector.IsConnected)
    {
      Connector.Disconnect();
      Connector.Dispose();
      Connector = null;
    }
  }

  public async Task<IConnector> Connect<TConnector, TConnection, TConnectorOptions>
  (Action<TConnectorOptions> optionsConfiguration = null!)
    where TConnector : ConnectorBase<TConnection, TConnectorOptions>, new()
    where TConnection : Connection
    where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
   => await Connect<TConnector, TConnection, TConnectorOptions>(
        new TConnectorOptions(),
        optionsConfiguration);

  public async Task<IConnector> Connect<TConnector, TConnection, TConnectorOptions>
  (TConnectorOptions? options = default,
  Action<TConnectorOptions> optionsConfiguration = null!)
    where TConnector : ConnectorBase<TConnection, TConnectorOptions>, new()
    where TConnection : Connection
    where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
  {
    options ??= new TConnectorOptions();
    optionsConfiguration?.Invoke(options);
    if (Connector != null)
    {
      Disconnect();
    }
    var connector = options.CreateConnector<TConnector, TConnection>(this);
    return await Connect<TConnector, TConnection, TConnectorOptions>(connector);
  }

  internal async Task<IConnector> Connect<TConnector, TConnection, TConnectorOptions>
  (IConnector<TConnection, TConnectorOptions>? connector = null)
    where TConnector : ConnectorBase<TConnection, TConnectorOptions>, new()
    where TConnection : Connection
    where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
  {
    Console.WriteLine($"ConnectedDocument.Connect():\n\tconnector={connector}\n\tConnector={connector}");
    Connector = connector;
    await Connector!.Connect(); // if already connected, simply returns
    Debug.Assert(Connector != null);
    return Connector!;
  }

  public void Disconnect()
  {
    if (Connector != null)
    {
      // Connector.Disconnect();
      // Connector.Document = null;
      Connector = null;
    }
  }

  public new TValue Get<TValue>(string key = "")
      where TValue : class =>
          (TValue)GetMap().Get(key);

  public void Set(string key, object value) =>
      base.Transact((tr) => GetMap().Set(key, value), this, true);

  public string ValuesToString() => YMapExtensions.ToString(GetMap());
  public override string ToString() => ToString();
  public string ToString(string? suffix = null) =>
    $"[{GetType().Name} Name={Name} ClientId={ClientId} map.Count={GetMap().Count}]" + (suffix != null ? " " + suffix : "");

  public void HandleDocumentUpdate(object? sender, (byte[] data, object origin, Transaction transaction) e)
  {
    Console.WriteLine(ToString($".HandleDocumentUpdate():\n\tsender={sender}\n\te.data={SyncProtocol.EncodeBytes(e.data)}\n\tthis={this}\n\torigin={e.origin}\n\ttransaction={e.transaction}\n\tConnector={Connector}"));
    if (e.data == null || e.data.Length == 0)
    {
      return;
    }

    if (Connector != null && Connector.IsConnected)
    {
      if (sender != null && e.origin == sender)
      {
        Connector.Broadcast(connection =>
        {
          Console.WriteLine(ToString($".HandleDocumentUpdate().Broadcast: connection={connection}"));
          connection.WriteUpdate(EncodeStateAsUpdateV2()); // e.data); // TODO: Or encode state vector/update using state vector , and broadcast that ??
        });
      }
    }
  }
}

public static class YDocExtensions
{
  public static string ToString(this YDoc doc) =>
    $"[{doc.GetType()} {(doc is ConnectedDocument ? (doc as ConnectedDocument)!.Name : "YDoc")}]: {/*YMapExtensions.ToString(*/doc.GetMap()/*)*/}";
}