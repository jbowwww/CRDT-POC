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

      Console.WriteLine($"PRECON doc1={doc1}"); //\nConnector={connector}\n
      await doc1.Connect<TcpConnector, TcpConnection, TcpConnectorOptions>(options => options.Parse(args));
      //var doc2 = new ConnectedDocument("Document #2");
      Console.WriteLine($"POSTCON doc1={doc1}");

      var isPrimaryNode = doc1.Connector.ConnectionId.EndsWith("1");
      if (isPrimaryNode)
      {
        await Task.Delay(500);
      }
      doc1.Set("prop1", isPrimaryNode ? "stringValue1" : "stringValue2");
      // doc2.Set("prop2", "stringValue2");
      Console.WriteLine($"POSTVAR doc1={doc1}");

      if (!isPrimaryNode)
      {
        var timer = new Timer(4000) { AutoReset = false };
        timer.Elapsed += (object? sender, ElapsedEventArgs e) =>
        {
          Console.WriteLine($"PRETIMER doc1={doc1}");
          doc1.Set("propTimer", "timerValue");
          doc1.Set("prop1", "prop1timered");
          Console.WriteLine($"POSTTIMER doc1={doc1}");
        };
        timer.Start();
      }

      Console.WriteLine($"PREEXIT doc1={doc1}");
      Console.ReadKey();
      Console.WriteLine($"POSTEXIT doc1={doc1}");
    }
  }
}
