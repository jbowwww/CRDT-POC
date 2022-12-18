using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Text;
using Ycs;
using Ycs.Hubs;
using Ycs.Middleware;

namespace Aemo
{
    public static class CrdtPoc
    {
        // probably not needed
        // private static YDoc? GlobalRoot;
        
        private static EventHandler<(byte[], object, Transaction)> CreateDocUpdateHandler(YDoc destDoc, string srcName = "AnonymousDoc") =>
            (object? sender, (byte[] data, object origin, Transaction transaction) eventArgs) =>
            {
                Console.WriteLine($"{srcName}.UpdateHandler():\n\tsender={sender}\n\tdata=byte[{eventArgs.data.Length}] " +
                $"{{{Encoding.ASCII.GetString(eventArgs.data)}}}\n\t" +
                $"transaction.origin={eventArgs.transaction.Origin}\n\tdestDoc={destDoc}");//\n\tGlobalRoot={GlobalRoot}");
                var stateUpdateV2 = (sender as YDoc)?.EncodeStateAsUpdateV2(eventArgs.data);
                // var stateVector = (sender as YDoc)?.EncodeStateVectorV2();
                destDoc.ApplyUpdateV2(eventArgs.data, eventArgs.transaction.Origin, false);
                Console.WriteLine($"{srcName}.UpdateHandler(): destDoc={destDoc}");//\n\tGlobalRoot={GlobalRoot}");
            };

        public static void Main()
        {
            // GlobalRoot = YcsManager.Instance.YDoc;
            // var hub = YcsHubAccessor.Instance.YcsHub;
            // var mgr = YcsManager.Instance;

            var doc1 = new ConnectedDocument("Document #1"/*, new YDocOptions() {}*/);
            var doc2 = new ConnectedDocument("Document #2");
            
            // YcsManager.Instance.HandleClientConnected(doc1.Guid); 
            // YcsManager.Instance.HandleClientConnected(doc2.Guid); 
            
            // mgr?.EnqueueAndProcessMessagesAsync(doc1.Guid, 0, new MessageToProcess() { Command = YjsCommandType.GetMissing, });
            // mgr?.EnqueueAndProcessMessagesAsync(doc2.Guid, 0, new MessageToProcess() { Command = YjsCommandType.GetMissing, });

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");//globalRoot={GlobalRoot}");

            // doc1.UpdateV2 += CreateDocUpdateHandler(doc2, nameof(doc1));
            // doc2.UpdateV2 += CreateDocUpdateHandler(doc1, nameof(doc2));

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");

            doc1.Set("prop1", "stringValue1");
            doc2.Set("prop2", "stringValue2");

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");
        }
    }
}
