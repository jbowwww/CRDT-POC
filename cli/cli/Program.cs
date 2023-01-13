using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Aemo.Connectors;
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

    public static async Task Main(string[] args)
    {
      // GlobalRoot = YcsManager.Instance.YDoc;
      // var hub = YcsHubAccessor.Instance.YcsHub;
      // var mgr = YcsManager.Instance;

      var doc1 = new ConnectedDocument("Document #1"/*, new YDocOptions() {}*/);
      //var doc2 = new ConnectedDocument("Document #2");

      Console.WriteLine($"PRECON doc1={doc1}");

      var connector = new TcpConnector(doc1, new TcpConnectorOptions(args));
      connector.Connect();

      var isPrimaryNode = connector.ConnectionId.EndsWith("1");

      Console.WriteLine($"POSTCON doc1={doc1}\nConnector={connector}\n");

      // YcsManager.Instance.HandleClientConnected(doc1.Guid); 
      // YcsManager.Instance.HandleClientConnected(doc2.Guid); 

      // mgr?.EnqueueAndProcessMessagesAsync(doc1.Guid, 0, new MessageToProcess() { Command = YjsCommandType.GetMissing, });
      // mgr?.EnqueueAndProcessMessagesAsync(doc2.Guid, 0, new MessageToProcess() { Command = YjsCommandType.GetMissing, });

      // Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");//globalRoot={GlobalRoot}");

      // doc1.UpdateV2 += CreateDocUpdateHandler(doc2, nameof(doc1));
      // doc2.UpdateV2 += CreateDocUpdateHandler(doc1, nameof(doc2));

      // Console.WriteLine($"doc1={doc1}\ndoc2={doc2}");

      if (isPrimaryNode)
      {
        await Task.Delay(500);
      }

      doc1.Set("prop1", isPrimaryNode ? "stringValue1" : "stringValue2");
      // doc2.Set("prop2", "stringValue2");

      Console.WriteLine($"POSTVAR doc1={doc1}\nConnector={connector}\n");

      if (isPrimaryNode)
      {
        var timer = new Timer(2000);
        timer.Elapsed += (object? sender, ElapsedEventArgs e) =>
        {
          doc1.Set("propTimer", "timerValue");
          Console.WriteLine($"POSTTIMER doc1={doc1}\nConnector={connector}\n");
        };
        timer.Start();
      }

      Process.GetCurrentProcess().WaitForExit();
      Console.WriteLine($"POSTEXIT doc1={doc1}\nConnector={connector}\n");
    }
  }
}
