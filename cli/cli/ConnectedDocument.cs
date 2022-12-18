using System;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using Ycs;
using Aemo.Connectors;

namespace Aemo;

public class ConnectedDocument : YDoc
{
    public class Options : YDocOptions
    {
        private readonly int _anonymousDocCount = 0;

        public Options()
        {
            if (string.IsNullOrEmpty(Name))
                Name = Name ?? $"AnonDoc #{++_anonymousDocCount}";
        }

        public string Name;
        public IConnector? Connector;
    }

    public ConnectedDocument(string name, Options? options = null)
        : base(options)
    {
        options ??= new Options();

        // make this new doc a top level child of global root doc - use Name
        // Not necessary apparenty. The main thing I had been missing was in the UpdateV2
        // handler on docs, need to sender.EncodeStateAsUpdateV2() (see update handler below)
        // Although doing this (adding to a global root doc) might remove the need for that,
        // within a single process.
        // TODO: Move adding the UpdateV2 handler to here. May need to consider destination doc(s)
        //YcsManager.Instance.YDoc?.GetMap().Set(Name, this);
        if (options.Connector != null)
            Connect(options.Connector);
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

    public ConnectedDocument Connect(IConnector? connector = null)
    {
        if (Connector != null && Connector != connector)
        {
            Disconnect();
        }
        Connector = connector;
        if (Connector != null)
        {
            SyncProtocol.WriteSyncStep1(Connector.TxStream, this);
            SyncProtocol.ReadSyncMessage(Connector.RxStream, Connector.TxStream, this, this);
            byte[] stateVector = EncodeStateVectorV2();
            byte[] stateUpdate = EncodeStateAsUpdateV2(stateVector);
            // SyncProtocol.WriteUpdate(Connector.TxStream, stateUpdate);
        }
        AddUpdateHandler((object? sender, (byte[] data, object origin, Transaction transaction) eventArgs) =>
        {
            Console.WriteLine($"{Name}.UpdateHandler():\n\tConnector={Connector}\n\tsender={sender} / this={this}\n\t" +
                $"data=byte[{eventArgs.data.Length}] {{ {Encoding.ASCII.GetString(eventArgs.data)} }}\n\t" +
                $"transaction.origin={eventArgs.transaction.Origin}\n\t");
            var stateVector = (sender as YDoc)?.EncodeStateVectorV2();
            var stateUpdate = (sender as YDoc)?.EncodeStateAsUpdateV2(stateVector);
            SyncProtocol.WriteUpdate(Connector?.TxStream, stateUpdate);
            SyncProtocol.ReadSyncMessage(Connector?.RxStream, Connector?.TxStream, this, this);
        });
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

    public void AddUpdateStreamHandler(PipeReader reader, PipeWriter writer)
    {
    }

    public void AddUpdatePipeHandler(IDuplexPipe pipe) => AddUpdateStreamHandler(pipe.Input, pipe.Output);
}

public static class YDocExtensions
{
    public static string ToString(this YDoc doc) =>
            $"[{doc.GetType()} {(doc is ConnectedDocument ? (doc as ConnectedDocument)!.Name : "YDoc")}]: {/*YMapExtensions.ToString(*/doc.GetMap()/*)*/}";
}