using System;
using System.Threading.Tasks;
using System.Timers;
using Aemo;
using Aemo.Connectors;

namespace cli
{
  public static class CrdtPoc
  {
    // Run instructions: Seen to be working, run 2 instances of this CLI program:
    // - "reset ; ./app/cli 127.0.0.1:2222 127.0.0.1:2221"
    // - "reset ; ./app/cli 127.0.0.1:2221 127.0.0.1:2222"
    //TODO: Remove Console.WriteLine's pasted repeatedly, add a doc1.Update handler to do it and count #updates(local/not?)
    public static async Task Main(string[] args)
    {
      var doc1 = new ConnectedDocument(/*, new YDocOptions() {}*/) { Name = "Document #1" };
      _ = await doc1.Connect<TcpConnector, TcpConnection, TcpConnectorOptions>(options => options.Parse(args)); // TODO: Change return value to IConnector if need to continue using doc1.Connector.* e.g. .ConnectionId. Saves getting out of doc1.
      bool isPrimaryNode = doc1?.Connector?.ConnectionId.EndsWith("1") ?? throw new InvalidOperationException($"doc1(={doc1}) or doc1.Connector(={doc1?.Connector}) is null");
      Console.WriteLine($"INIT isPrimaryNode={isPrimaryNode} doc1={doc1}: {doc1.ValuesToString()}");
      if (isPrimaryNode)
      {
        await Task.Delay(500);
      }

      // TODO: Try doc.Transact()
      doc1.Set("prop1", isPrimaryNode ? "stringValue1" : "stringValue2");
      doc1.Set(isPrimaryNode ? "prop2-1" : "prop2-2", isPrimaryNode ? "stringValue1" : "stringValue2");
      doc1.Set("prop3-1", isPrimaryNode ? "stringValue1" : "stringValue2");
      doc1.Set("prop3-2", isPrimaryNode ? "stringValue1" : "stringValue2");
      doc1.Set(isPrimaryNode ? "prop4-1" : "prop4-2", isPrimaryNode ? "stringValue1" : "stringValue2");
      doc1.Set(isPrimaryNode ? "prop4-2" : "prop4-1", isPrimaryNode ? "stringValue1" : "stringValue2");
      Console.WriteLine($"SET doc1={doc1}: {doc1.ValuesToString()}");

      if (!isPrimaryNode)
      {
        var timer = new Timer(4000) { AutoReset = false };
        timer.Elapsed += (sender, e) =>
        {
          doc1.Set("propTimer", "timerValue");
          doc1.Set("prop1", "prop1timered");
          Console.WriteLine($"TIMER doc1={doc1}: {doc1.ValuesToString()}");
        };
        timer.Start();
      }

      _ = Console.ReadKey();
      Console.WriteLine($"POSTEXIT doc1={doc1}: {doc1.ValuesToString()}");
    }
  }
}
