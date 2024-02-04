using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aemo.Connectors;

namespace cli.Connectors
{
  public class ConnectionDictionary<TConnection>
      : ConcurrentDictionary<string, TConnection>,
        IDictionary<string, TConnection>,
        ICollection<TConnection>
    where TConnection : IConnection
  {
    public bool IsReadOnly => false;

    TConnection IDictionary<string, TConnection>.this[string connectionId]
    {
      get => base[connectionId];
      set => base[connectionId] = value;
    }

    public override string ToString() => $"{Count} connections, {Values.Count(c => c.Status <= ConnectionStatus.Connected)} active";

    public void Add(TConnection connection)
    {
      try
      {
        AddOrUpdate(
        connection.ConnectionId,
        (connectionId) =>
        {
          Console.WriteLine($"Listening to connection={connection}, listening ...");
          return connection;
        },
        (connectionId, oldConnection) =>
        {
          Console.WriteLine($"Replaced and listening to new connection={connection}\n\toldConnection={oldConnection} ...");
          oldConnection.Stream.Flush();
          oldConnection.Stream.Close();
          oldConnection.Stream.Dispose();
          return connection;
        });
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Cannot add TConnection={typeof(TConnection).Name} item={connection}", ex);
      }
    }

    public bool Contains(TConnection item) => ContainsKey(item.ConnectionId);

    public void CopyTo(TConnection[] array, int arrayIndex)
    {
      Debug.Assert(array.Length >= arrayIndex + Keys.Count);
      Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(TConnection item) => TryRemove(item.ConnectionId, out _);

    IEnumerator<TConnection> IEnumerable<TConnection>.GetEnumerator() => Values.GetEnumerator();

    // public TConnection CreateFromSocket<TConnector, TConnectorOptions>(Socket socket, TConnector connector)
    //   where TConnector : ConnectorBase<TConnection, TConnectorOptions>, new()
    //   where TConnectorOptions : ConnectorOptions<TConnectorOptions>, new()
    // {
    //   var connection = new TConnection()
    //   {
    //     Connector = connector,
    //     Socket = socket,
    //     Stream = new NetworkStream(socket, true),
    //   };

    //   return connection;
    // }
  }
}