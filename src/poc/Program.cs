using System;
using System.Threading.Tasks;
using System.Timers;
using Aemo.Connectors;
using Ycs;

namespace Poc;

public static class CrdtPoc
{
    // Run instructions: Seen to be working, run 2 instances of this CLI program:
    // - "reset ; ./app/cli 127.0.0.1:2222 127.0.0.1:2221"
    // - "reset ; ./app/cli 127.0.0.1:2221 127.0.0.1:2222"
    // Note: The last digit of the instance's listening port number is tested to see if it is 1 ;
    // if so, that instance is nominally the 'primary' node (isPrimaryNode = true)
    // This is used to perform slightly different write operations on the primary node (or not)
    //TODO: Remove Console.WriteLine's pasted repeatedly, add a doc1.Update handler to do it and count #updates(local/not?)
    public static async Task Main(string[] args)
    {
        var doc1 = new YDoc(/*, new YDocOptions() {}*/);
        using (var connector = await doc1.Connect<TcpConnector, TcpConnectorOptions>(new TcpConnectorOptions(args)))
        {
            bool isPrimaryNode = connector.Options.IsPrimary;
            Console.WriteLine($"INIT isPrimaryNode={isPrimaryNode} doc1={doc1.ToString(true)}");
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
            Console.WriteLine($"SET doc1={doc1.ToString(true)}");

            if (!isPrimaryNode)
            {
                var timer = new Timer(4000) { AutoReset = false };
                timer.Elapsed += (sender, e) =>
                {
                    doc1.Set("propTimer", "timerValue");
                    doc1.Set("prop1", "prop1timered");
                    Console.WriteLine($"TIMER doc1={doc1.ToString(true)}");
                };
                timer.Start();
            }

            _ = Console.ReadKey();
            Console.WriteLine($"POSTEXIT doc1={doc1.ToString(true)}");
        }
    }
}
