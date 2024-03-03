using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aemo.Connectors;

namespace cli.Connectors
{
  public class ConnectionDictionary
  : ConcurrentDictionary<string, IConnection>,
    IDictionary<string, IConnection>,
    ICollection<IConnection>
  {
    public bool IsReadOnly => false;

    IConnection IDictionary<string, IConnection>.this[string connectionId]
    {
      get => base[connectionId];
      set => base[connectionId] = value;
    }

    public override string ToString() => $"{Count} connections, {Values.Count(c => c.Status <= ConnectionStatus.Connected)} active";

    public void Add(IConnection connection)
    {
      try
      {
        AddOrUpdate(
        connection.Id,
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
        throw new InvalidOperationException($"Cannot add TConnection={connection.GetType().Name} item={connection}", ex);
      }
    }

    public bool Contains(IConnection item) => ContainsKey(item.Id);

    public void CopyTo(IConnection[] array, int arrayIndex)
    {
      Debug.Assert(array.Length >= arrayIndex + Keys.Count);
      Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(IConnection item) => TryRemove(item.Id, out _);

    IEnumerator<IConnection> IEnumerable<IConnection>.GetEnumerator() => Values.GetEnumerator();
  }
}