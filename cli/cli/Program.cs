using System;
using System.Text;
using Ycs;

public static class YMapExtensions
{
    public static string ToString(this YMap map)
    {
        StringBuilder sb = new StringBuilder("\n");
        foreach (var entry in map)
        {
            sb.Append($"\t{entry.Key}={entry.Value}");
        }
        return sb.ToString();
    }
}

public class YDocEx : YDoc
{
    public YMap Get() => this.GetMap();

    public new TValue? Get<TValue>(string name = "") where TValue : class => base.GetMap().Get(name) as TValue;
    
    public void Set(string name, object value) => base.GetMap().Set(name, value);

    public override string ToString()
    {
        return $"[{this.GetType()}: {YMapExtensions.ToString(this.GetMap())}]";
    }
}

public class AemoCrdtPoc
{
    public static void Main()
    {
        var doc1 = new YDocEx(/*new YDocOptions() {}*/);
        var doc2 = new YDocEx();

        EventHandler<(byte[], object, Transaction)> UpdateHandler(YDoc doc, string docName = "AnonymousDoc") =>
            (object? sender, (byte[] data, object origin, Transaction transaction) eventArgs) =>
            {
                bool local = eventArgs.transaction.Origin == doc;
                if (local)
                {
                    doc.ApplyUpdateV2(eventArgs.data, eventArgs.transaction.Origin, local);
                }
                Console.WriteLine($"{docName}.UpdateHandler(): eventArgs={eventArgs}");
            };

        Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");

        doc1.Set("name", "Document #1");
        doc1.UpdateV2 += UpdateHandler(doc2, nameof(doc1));

        doc2.Set("name", "Document #2");
        doc2.UpdateV2 += UpdateHandler(doc1, nameof(doc2));

        Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");

        doc1.Set("prop1", "stringValue1");
        doc2.Set("prop2", "stringValue2");

        Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");

    }}
