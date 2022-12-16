using System;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Text;
using Ycs;
using Ycs.Hubs;
using Ycs.Middleware;

namespace Aemo
{
    public static class YMapExtensions
    {
        public static string ToString(this YMap map)
        {
            StringBuilder sb = new StringBuilder("");//("\n");
            foreach (var entry in map)
            {
                sb.Append($"\t{entry.Key}={entry.Value}");
            }
            return sb.ToString();
        }
    }

    public class YDocRoot : YDoc
    {
        readonly int anonymousDocCount = 0;

        public YDocRoot(string name, YDocOptions? options = null)
            : base(options)
        {
            // Set property "name" on this doc
            Set("name", name ?? $"AnonDoc #{++anonymousDocCount}");
            // make this new doc a top level child of global root doc - use Name
            // Not necessary apparenty. The main thing I had been missing was in the UpdateV2
            // handler on docs, need to sender.EncodeStateAsUpdateV2() (see update handler below)
            // Although doing this (adding to a global root doc) might remove the need for that,
            // within a single process.
            // TODO: Move adding the UpdateV2 handler to here. May need to consider destination doc(s)
            //YcsManager.Instance.YDoc?.GetMap().Set(Name, this);
        }

        // Convenience property gets Name out of the document
        // TODO: rename document prop from "name" to "Name"
        public string Name { get => Get<string>("name"); }

        public new TValue Get<TValue>(string name = "")
            where TValue : class =>
                (TValue)GetMap().Get(name);

        public void Set(string name, object value) =>
            base.Transact((tr) => GetMap().Set(name, value), this, true);

        public override string ToString()
        {
            return $"[{GetType()} {Name}]: {YMapExtensions.ToString(GetMap())}";
        }

    }
    
    public static class YDocExtensions
    {
        public static string ToString(this YDoc doc) =>
             $"[{doc.GetType()} {(doc is YDocRoot ? (doc as YDocRoot)!.Name : "YDoc")}]: {YMapExtensions.ToString(doc.GetMap())}";
    }

    public static class CrdtPoc
    {
        // probably not needed
        // private static YDoc? GlobalRoot;
        
        private static EventHandler<(byte[], object, Transaction)> CreateDocUpdateHandler(YDoc destDoc, string srcName = "AnonymousDoc") =>
            (object? sender, (byte[] data, object origin, Transaction transaction) eventArgs) =>
            {
                Console.WriteLine($"{srcName}.UpdateHandler():\n\tsender={sender}\n\tdata=byte[{eventArgs.data.Length}] " +
                $"{{{Encoding.ASCII.GetString(eventArgs.data)}}}\n\t" +
                $"transaction.origin={eventArgs.transaction.Origin}\n\tdestDoc={destDoc}\n\tGlobalRoot={GlobalRoot}");
                var stateUpdateV2 = (sender as YDoc)?.EncodeStateAsUpdateV2(eventArgs.data);
                // var stateVector = (sender as YDoc)?.EncodeStateVectorV2();
                destDoc.ApplyUpdateV2(eventArgs.data, eventArgs.transaction.Origin, false);
                Console.WriteLine($"{srcName}.UpdateHandler(): destDoc={destDoc}\n\tGlobalRoot={GlobalRoot}");
            };

        public static void Main()
        {
            // GlobalRoot = YcsManager.Instance.YDoc;
            // var hub = YcsHubAccessor.Instance.YcsHub;
            // var mgr = YcsManager.Instance;

            var doc1 = new YDocRoot("Document #1"/*, new YDocOptions() {}*/);
            var doc2 = new YDocRoot("Document #2");
            
            // YcsManager.Instance.HandleClientConnected(doc1.Guid); 
            // YcsManager.Instance.HandleClientConnected(doc2.Guid); 
            
            // mgr?.EnqueueAndProcessMessagesAsync(doc1.Guid, 0, new MessageToProcess() { Command = YjsCommandType.GetMissing, });
            // mgr?.EnqueueAndProcessMessagesAsync(doc2.Guid, 0, new MessageToProcess() { Command = YjsCommandType.GetMissing, });

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}globalRoot={GlobalRoot}");

            doc1.UpdateV2 += CreateDocUpdateHandler(doc2, nameof(doc1));
            doc2.UpdateV2 += CreateDocUpdateHandler(doc1, nameof(doc2));

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");

            doc1.Set("prop1", "stringValue1");
            doc2.Set("prop2", "stringValue2");

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");
        }
    }
}
