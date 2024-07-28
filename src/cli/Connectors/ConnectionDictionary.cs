using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace cli.Connectors
{
    public class ConnectionDictionary<TConnection>
      : ConcurrentDictionary<string, TConnection>,
        IDictionary<string, TConnection>,
        ICollection<TConnection>,
        IEnumerable<TConnection>
    where TConnection : IConnection
    {
        public bool IsReadOnly => false;

        public enum ChangeType { Add, Remove, Update };

        public class ChangeEventArgs : EventArgs
        {
            public readonly ChangeType Change;
            public readonly string Key;
            public readonly TConnection Connection;
            public ChangeEventArgs(ChangeType changeType, string key, TConnection connection)
            {
                Change = changeType;
                Key = key;
                Connection = connection;
            }
        }

        public delegate void ChangeEventHandler(object sender, ChangeEventArgs e);

        public event ChangeEventHandler? Change;

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
                var change = AddOrUpdate(
                  connection.Id,
                  (connectionId) =>
                  {
                      Change?.Invoke(this, new ChangeEventArgs(ChangeType.Add, connection.Id, connection));
                      return connection;
                  },
                  (connectionId, oldConnection) =>
                  {
                      Change?.Invoke(this, new ChangeEventArgs(ChangeType.Update, connection.Id, connection));
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

        public bool Contains(TConnection item) => ContainsKey(item.Id);

        public void CopyTo(TConnection[] array, int arrayIndex)
        {
            Debug.Assert(array.Length >= arrayIndex + Keys.Count);
            Values.CopyTo(array, arrayIndex);
        }

        public bool Remove(TConnection item)
        {
            if (TryRemove(item.Id, out _))
            {
                Change?.Invoke(this, new ChangeEventArgs(ChangeType.Remove, item.Id, item));
                return true;
            }
            return false;
        }

        IEnumerator<TConnection> IEnumerable<TConnection>.GetEnumerator() => Values.GetEnumerator();

        public IEnumerable<TConnection> ValidConnections => Values.Where(connection => connection.Status != ConnectionStatus.Disconnected);
        public bool HasValidConnections => ValidConnections.Count() > 0;
        public IEnumerable<TConnection> DataPending => Values.Where(connection => connection.IsDataAvailable);
    }
}