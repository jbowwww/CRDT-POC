# CRDT-POC

## eventually **C**onsistent **R**eplicated **D**ata **T**ypes - Proof of Concept

CRDTs are one of the more interesting ideas and modern concepts to appear in recent years.

This code may or may not work - it's experimental & exploratory - but hopefully it does and I can formally prove/verify its correctness with some tests.

For now I need to understand the inner workings of Ycs better.

**TcpConnector**: A TCP protocol implementation for Ycs.

This project essentially uses Ycs, via a pure TCP sockets protocol layer implementation, and some YDoc extensions syntactic sugar.

Use through the provided YDocExtensions.Connect() like this:

```c#
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

## Build & Run

Simply

```bash
docker compose up --build
```

## Analysis & Verification

I would like to write some tests to verify the simplest of my expectations. For starters:

- assert that all nodes are in fact, eventually consistent (i.e. all document property values match).
    > [!NOTE] 
    > This should be true for all nodes, unless any of them disconnect earlier than the others (e.g. due to the Task.Delay()'s varying across nodes - just include a long enough Delay() on all nodes before reaching the end of the using(doc1.Connect<>()) {} block, and therefore disconnecting.
