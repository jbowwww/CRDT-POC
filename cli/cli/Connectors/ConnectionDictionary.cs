using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Aemo.Connectors;

public class ConnectionDictionary<TConnection>
    // : ConcurrentDictionary<string, TConnection>
    : KeyedCollection<string, TConnection>
  where TConnection : IConnection
{
  public ConnectionDictionary()
  {

  }

  protected override string GetKeyForItem(TConnection item) => item.ConnectionId;

  // public void Add(string id, TConnection connection)
  // {
  //   var newConnection = base.AddOrUpdate(
  //     id,
  //     id => connection,
  //     (id, connection) => throw new InvalidOperationException($"Try to AddOrUpdate existing key in Connections ConcurrentDictionary"));
  // }
}
