using System;
using System.Net.NetworkInformation;
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
            Set("name", name ?? $"AnonymousDoc #{++anonymousDocCount}");
            YcsManager.Instance.YDoc.GetMap().Set(Name, this);
        }

        public string Name { get => Get<string>("name"); }

        public new TValue Get<TValue>(string name = "")
            where TValue : class =>
                (TValue)base.GetMap().Get(name);

        public void Set(string name, object value) =>
            base.GetMap().Set(name, value);

        public override string ToString()
        {
            return $"[{this.GetType()} {this.Name}]: {YMapExtensions.ToString(this.GetMap())}";
        }
    }

    public static class CrdtPoc
    {
        private static EventHandler<(byte[], object, Transaction)> CreateDocUpdateHandler(YDoc destDoc, string srcName = "AnonymousDoc") =>
            (object? sender, (byte[] data, object origin, Transaction transaction) eventArgs) =>
            {
                Console.WriteLine($"{srcName}.UpdateHandler():\n\tsender={sender}\n\tdata=byte[{eventArgs.data.Length}] " +
                $"{{{Encoding.ASCII.GetString(eventArgs.data)}}}\n\t" +
                $"transaction.origin={eventArgs.transaction.Origin}\ndestDoc={destDoc}");
                // var stateUpdateV2 = (sender as YDocRoot).EncodeStateAsUpdateV2(eventArgs.data);
                // var stateVector = (sender as YDocRoot).EncodeStateVectorV2();
                destDoc.ApplyUpdateV2(eventArgs.data, sender, false);
                Console.WriteLine($"{srcName}.UpdateHandler(): destDoc={destDoc}");
            };

        public static void Main()
        {
            var doc1 = new YDocRoot("Document #1"/*, new YDocOptions() {}*/);
            var doc2 = new YDocRoot("Document #2");
            // YcsManager.Instance.HandleClientConnected(doc1.Guid);

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");

            doc1.UpdateV2 += CreateDocUpdateHandler(doc2, nameof(doc1));
            doc2.UpdateV2 += CreateDocUpdateHandler(doc1, nameof(doc2));

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");

            doc1.Set("prop1", "stringValue1");
            doc2.Set("prop2", "stringValue2");

            Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");
        }
    }
}
