# CRDT-POC

eventually **C**onsistent **R**eplicated **D**ata **T**ypes (CRDTs), IMHO, are one of the more interesting ideas and modern concepts to appear in recent years.

This code may or may not work - it's experimental & exploratory - but hopefully it does and I can formally prove/verify its correctness with some tests.

For now I need to understand the inner workings of Ycs better.

**TcpConnector**: This project essentially uses Ycs, via a pure TCP sockets protocol layer implementation, and some YDoc extensions syntactic sugar.

Use the provided YDocExtensions.Connect() like this:

```c#
public static async Task Main(string[] args)
{
    Console.WriteLine($"START");
    var doc1 = new YDoc(/*, new YDocOptions() {}*/);
    var options = OptionsParser<TcpConnectorOptions>.Parse(args);
    using (var connector = await doc1.Connect<TcpConnector>(options))
    {
        doc1.Set("prop1", "value1");
        doc1.Set("prop2", 2);
        doc1.Set("prop3", 3.0d);

        await Task.Delay(2500);
    }
    
    await Task.Delay(2000);
    Console.WriteLine($"EXIT: doc1={doc1.ToString(doc1.ValuesToString())}");
}
```

## Build & Run

Simply

```bash
docker compose up --build
```

Configuration of nodes (currently testing with 4) is done in docker-compose.yaml with environment variables that get passed via command line in the container image. Each node should be specified by something like this:

```docker-compse
  cli-1:
    image: crdt-poc:latest
    build: .
    environment:
      HOST: cli-1
      PORT: 2221
      REMOTE_LIST: cli-2:2221 cli-3:2221 cli-4:2221**
```

$HOST:$PORT is the endpoint that node will listen on, and $REMOTE_LIST are the other nodes to connect to.

## Analysis & Verification

I would like to write some tests to verify the simplest of my expectations. For starters:

- assert that all nodes are in fact, eventually consistent (i.e. all document property values match).
    > [!NOTE] 
    > This should be true for all nodes, unless any of them disconnect earlier than the others (e.g. due to the Task.Delay()'s varying across nodes - just include a long enough Delay() on all nodes before reaching the end of the using(doc1.Connect<>()) {} block, and therefore disconnecting.
