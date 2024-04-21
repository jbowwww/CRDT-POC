# CRDT-POC

**TcpConnector**: A TCP protocol implementation for Ycs.

Use through the provided YDocExtensions.Connect() like this:

```plain
public static async Task Main(string[] args)
{
    var doc1 = new YDoc(/*, new YDocOptions() {}*/);    // ConnectedDocument( { Name = "Document #1" };
    using (var connector = await doc1.Connect<TcpConnector, TcpConnectorOptions>(options => options.Parse(args)))
    {
        bool isPrimaryNode = connector.Id.EndsWith("1");
        // TODO: Try doc.Transact()
        doc1.Set("prop1", isPrimaryNode ? "stringValue1" : "stringValue2");
        doc1.Set(isPrimaryNode ? "prop2-1" : "prop2-2", isPrimaryNode ? "stringValue1" : "stringValue2");
        doc1.Set("prop3-1", isPrimaryNode ? "stringValue1" : "stringValue2");
        doc1.Set("prop3-2", isPrimaryNode ? "stringValue1" : "stringValue2");
        doc1.Set(isPrimaryNode ? "prop4-1" : "prop4-2", isPrimaryNode ? "stringValue1" : "stringValue2");
        doc1.Set(isPrimaryNode ? "prop4-2" : "prop4-1", isPrimaryNode ? "stringValue1" : "stringValue2");
    }
}
```
