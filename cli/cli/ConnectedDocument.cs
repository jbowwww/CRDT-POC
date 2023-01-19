using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Aemo.Connectors;
using Ycs;

namespace Aemo;

public class ConnectedDocument : YDoc
{
  public class Options : YDocOptions
  {
    private readonly int _anonymousDocCount = 0;

    public Options()
    {
      if (string.IsNullOrEmpty(Name))
        Name ??= $"AnonDoc #{++_anonymousDocCount}";
    }

    public string Name;
    public IConnector? Connector;
  }

  public ConnectedDocument(string name, Options? options = null)
      : base(options)
  {
    options ??= new Options();
    Name = $"Document #{ClientId}"; // name;
    // make this new doc a top level child of global root doc - use Name
    // Not necessary apparenty. The main thing I had been missing was in the UpdateV2
    // handler on docs, need to sender.EncodeStateAsUpdateV2() (see update handler below)
    // Although doing this (adding to a global root doc) might remove the need for that,
    // within a single process.
    // TODO: Move adding the UpdateV2 handler to here. May need to consider destination doc(s)
    //YcsManager.Instance.YDoc?.GetMap().Set(Name, this);

    // if (options.Connector != null)
    //   Connect((options.Connector);

    AddUpdateHandler(HandleDocumentUpdate);

  }

  // Convenience property uses the Name key of the YDoc
  public string Name
  {
    get => Get<string>("Name");
    protected set => Set("Name", value);
  }

  public IConnector? Connector { get; internal set; } = null;

  public void Disconnect()
  {
    if (Connector != null)
    {
      Connector = null;
    }
  }

  public async Task<ConnectedDocument> Connect<TConnector, TConnection, TConnectorOptions>
  (Action<TConnectorOptions> optionsConfiguration = null!)
    // where TConnector : IConnector<TConnection, TConnectorOptions>, 
    // where TConnector : ConnectorBase<TConnector, TConnectorOptions>, new()
    where TConnector : ConnectorBase<TConnection, TConnectorOptions>, new()
    where TConnection : ConnectionBase, IConnection, new()
    where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
  {
    TConnectorOptions options = new();
    optionsConfiguration?.Invoke(options);
    var connector = options.CreateConnector<TConnector, TConnection>(this);
    var connectedDocument = await Connect(connector);
    return connectedDocument;
  }

  public async Task<ConnectedDocument> Connect<TConnection, TConnectorOptions>(IConnector<TConnection, TConnectorOptions>? connector = null)
    where TConnection : ConnectionBase, new()
    where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
  {
    Console.WriteLine($"ConnectedDocument.Connect(): connector={connector} Connector={connector}");
    if (Connector != null && Connector != connector)
    {
      Disconnect();
    }
    Connector = connector;
    if (Connector != null/*  && Connector.IsConnected */)
    {
      await Connector.Connect(); // if already connected, simply returns
      Console.WriteLine($"ConnectedDocument.Connect(): Broadcast WriteSyncStep1 for doc={this}");
      Connector?.Broadcast(stream => SyncProtocol.WriteSyncStep1(stream, this));
      _ = Task.Run(() =>
      {
        foreach (var connection in connector?.Connections!)  // why is ! necessary
        {
          Console.WriteLine($"ConnectedDocument.Connect(): ReadSyncMessage for connection={connection.ConnectionId}");
          var messageType = SyncProtocol.ReadSyncMessage(
            connection.Stream,
            connection.Stream,
            this, this);
          Console.WriteLine($"ConnectedDocument.Connect(): ReadSyncMessage for connection={connection.ConnectionId} returned {messageType}");

        }

      });
    }
    return this;
  }

  public new TValue Get<TValue>(string key = "")
      where TValue : class =>
          (TValue)GetMap().Get(key);

  public void Set(string key, object value) =>
      base.Transact((tr) => GetMap().Set(key, value), this, true);

  public override string ToString()
  {
    return $"[{GetType()} {Name}]: {YMapExtensions.ToString(GetMap())}";
  }

  public void AddUpdateHandler(EventHandler<(byte[] data, object origin, Transaction transaction)> eventHandler)
  {
    UpdateV2 += eventHandler;
  }

  public void HandleDocumentUpdate(object? sender, (byte[] data, object origin, Transaction transaction) e)
  {
    Console.WriteLine($"TcpConnector.HandleDocumentUpdate(): sender={sender} e.data={e.data} origin={e.origin} transaction={e.transaction} Connector?.Status={Connector?.Status}");
    if (e.data == null || e.data.Length == 0)
    {
      return;
    }

    if (Connector != null && Connector.IsConnected)
    {
      Connector.Broadcast(connection =>
      {
        Console.WriteLine($"TcpConnector.HandleDocumentUpdate().Broadcast: connection={connection} this={this} e.origin={e.origin} e,data={SyncProtocol.EncodeBytes(e.data)}");
        SyncProtocol.WriteUpdate(connection, EncodeStateAsUpdateV2()); // TODO: Or encode state vector/update using state vector , and broadcast that ??
      });
    }
  }

  public void AddUpdateDocumentHandler(YDoc destinationDocument)
  {
    AddUpdateHandler((object? sender, (byte[] data, object origin, Transaction transaction) eventArgs) =>
    {
      Console.WriteLine($"{Name}.UpdateHandler():\n\tsender={sender} / this={this}\n\t" +
          $"data=byte[{eventArgs.data.Length}] {{ {Encoding.ASCII.GetString(eventArgs.data)} }}\n\t" +
          $"transaction.origin={eventArgs.transaction.Origin}\n\tdestinationDocument={destinationDocument}");//\n\tGlobalRoot={GlobalRoot}");
      byte[] stateVector = EncodeStateVectorV2();
      byte[] stateUpdate = EncodeStateAsUpdateV2(stateVector);
      destinationDocument.ApplyUpdateV2(stateUpdate, eventArgs.origin, false);
      Console.WriteLine($"{Name}.UpdateHandler(): destinationDocument={destinationDocument}");//\n\tGlobalRoot={GlobalRoot}");
    });
  }

  public void AddUpdateStreamHandler(Stream reader, Stream writer)
  {
    SyncProtocol.ReadSyncMessage(reader, writer, this, this);
    AddUpdateHandler((object? sender, (byte[] data, object origin, Transaction transaction) eventArgs) =>
    {
      Console.WriteLine($"{Name}.UpdateHandler():\n\tsender={sender} / this={this}\n\t" +
              $"data=byte[{eventArgs.data.Length}] {{ {Encoding.ASCII.GetString(eventArgs.data)} }}\n\t" +
              $"transaction.origin={eventArgs.transaction.Origin}\n\t");
      var stateUpdateV2 = (sender as YDoc)?.EncodeStateAsUpdateV2(eventArgs.data);
      // var stateVector = (sender as YDoc)?.EncodeStateVectorV2();
      SyncProtocol.WriteUpdate(writer, stateUpdateV2);
    });
  }
}

public static class YDocExtensions
{
  public static string ToString(this YDoc doc) =>
          $"[{doc.GetType()} {(doc is ConnectedDocument ? (doc as ConnectedDocument)!.Name : "YDoc")}]: {/*YMapExtensions.ToString(*/doc.GetMap()/*)*/}";
}