using System;
using Ycs;

var doc1 = new YDoc(/*new YDocOptions() {}*/);
var doc2 = new YDoc();

EventHandler<(byte[], object, Transaction)> UpdateHandler(YDoc doc) =>
    (object? sender, (byte[] data, object origin, Transaction transaction) eventArgs) =>
    {
        bool local = eventArgs.transaction.Origin == doc;
        if (local)
        {
            doc.ApplyUpdateV2(eventArgs.data, eventArgs.transaction.Origin, local);
        }
        Console.WriteLine((doc == doc1 ? "doc1" : "doc2") + ".UpdateHandler(): eventArgs={eventArgs}");
    };

Console.WriteLine("doc1={doc1}\ndoc2={doc2}");

doc1.GetMap().Set("name", "Document #1");
doc1.UpdateV2 += UpdateHandler(doc2);

doc2.GetMap().Set("name", "Document #2");
doc2.UpdateV2 += UpdateHandler(doc1);

Console.WriteLine("doc1={doc1}\ndoc2={doc2}");
