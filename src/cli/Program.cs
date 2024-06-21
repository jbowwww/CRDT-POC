using System;
using System.Threading.Tasks;
using System.Timers;
using Ycs;
using cli.Connectors;
using System.Linq;

namespace cli;

public static class CrdtPoc
{
    // Now experimenting with docker-compose and 2-4 instances
    // I want to first implement some primitive method of testing 2-4 or more instances
    // Each instance extracts a unique nodeNumber from their hostnames, set in docker-compose.yaml
    // If I use this (similar to before with 2 nodes) to select both property names and values,
    // I should be able to confirm that all instances are transmitting & applying updates to each.
    // If I use microsecond timer values as part of the values, I can hopefully confirm
    // that the distributed updates are being applied at each node in the correct order.
    // (Since all instances are in docker, the timer values should all be perfectly synced)
    // Then you could use the nodeNumber's to delay the operations at each instance by a varied amount.
    // Obviously the latest written values should prevail (and obviously become consistent eventually)
    // THEN, you could try simulating network outages and/or delays (again, based on timer values should
    // mean expected, predictable, i.e. test-able outcomes )
    // Try to implement the ideas above (for starters..) in proper test cases.
    public static async Task Main(string[] args)
    {
        var doc1 = new YDoc(/*, new YDocOptions() {}*/);
        using (var connector = await doc1.Connect<TcpConnector, TcpConnectorOptions>(options => options.Parse(args)))
        {
            var nodeStart = DateTime.Now;
            var nodeName = connector.Id;
            int nodeNumber = int.Parse(nodeName.Last().ToString());

            var getMs = () => DateTime.Now.Subtract(nodeStart).TotalMicroseconds;
            var dbg = (string txt) => Console.WriteLine($"{getMs()} {txt}");

            // The higher the node number, the more dominant (i.e. more props and/or more often) that node's values should be in the end document
            //await Task.Delay(100 * nodeNumber);
            // TODO: Try doc.Transact()
            
            dbg($"START @ {nodeStart} nodeNumber={nodeNumber} doc1={doc1.ToString(doc1.ValuesToString())}");

            doc1.Set(nodeName + "-start", $"{getMs()}{(nodeStart - new DateTime(0)).TotalMicroseconds}");
            doc1.Set("shared-start", $"{getMs()}value-from-node{nodeNumber}");

            dbg($"PROP @ {nodeStart} nodeNumber={nodeNumber} doc1={doc1.ToString(doc1.ValuesToString())}");

            doc1.Set($"prop{nodeNumber}-1", $"{getMs()}value-from-node{nodeNumber}");
            doc1.Set($"prop{nodeNumber}-2", $"{getMs()}value-from-node{nodeNumber}");
            doc1.Set($"prop{nodeNumber}-3", $"{getMs()}value-from-node{nodeNumber}");
            doc1.Set($"prop{nodeNumber}-4", $"{getMs()}value-from-node{nodeNumber}");

            dbg($"DELAY @ {nodeStart} nodeNumber={nodeNumber} doc1={doc1.ToString(doc1.ValuesToString())}");

            await Task.Delay(100 * nodeNumber);

            doc1.Set($"delay{nodeNumber}-1", $"{getMs()}value-from-node{nodeNumber}");
            doc1.Set($"delay{nodeNumber}-2", $"{getMs()}value-from-node{nodeNumber}");
            doc1.Set($"delay{nodeNumber}-3", $"{getMs()}value-from-node{nodeNumber}");
            doc1.Set($"delay{nodeNumber}-4", $"{getMs()}value-from-node{nodeNumber}");

            dbg($"LAST doc1={doc1.ToString(doc1.ValuesToString())}");

            await Task.Delay(100 * nodeNumber);

            doc1.Set(nodeName + "-last", $"{getMs()}value-from-node{nodeNumber}");
            doc1.Set("shared-last", $"{getMs()}value-from-node{nodeNumber}");

            await Task.Delay(1000);
        }
        
        await Task.Delay(1000);

        Console.WriteLine($"POSTEXIT doc1={doc1.ToString(doc1.ValuesToString())}");
    }
}
