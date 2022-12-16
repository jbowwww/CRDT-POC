// ------------------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
// ------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Ycs;

namespace Aemo
{
    public class SyncDoc : YDoc
    {
        public YcsConnector Connector { get; internal set; }

        public IDictionary<SyncDoc, Queue<byte[]>> Messages = new ConcurrentDictionary<SyncDoc, Queue<byte[]>>();

        public SyncDoc(YcsConnector connector, YDocOptions options)
            
        {
            Connector = connector;
            Connector.Connections.Add(this);

            // Setup observe on local model.
            UpdateV2 += (s, e) =>
            {
                if (e.origin != Connector)
                {
                    using (var stream = new MemoryStream())
                    {
                        YcsProtocolInMemory.WriteUpdate(stream, e.data);
                        BroadcastMessage(this, stream.ToArray());
                    }
                }
            };

            Connect();
        }

        private void BroadcastMessage(SyncDoc sender, byte[] data)
        {
            if (Connector.OnlineConnections.Contains(sender))
            {
                foreach (var conn in Connector.OnlineConnections)
                {
                    if (sender != conn)
                    {
                        conn.Receive(data, sender);
                    }
                }
            }
        }

        public void Connect()
        {
            if (!Connector.OnlineConnections.Contains(this))
            {
                Connector.OnlineConnections.Add(this);

                using (var stream = new MemoryStream())
                {
                    YcsProtocolInMemory.WriteSyncStep1(stream, this);

                    // Publish SyncStep1
                    BroadcastMessage(this, stream.ToArray());

                    foreach (var remoteYInstance in Connector.OnlineConnections)
                    {
                        if (remoteYInstance != this)
                        {
                            stream.SetLength(0);
                            YcsProtocolInMemory.WriteSyncStep1(stream, remoteYInstance);

                            Receive(stream.ToArray(), remoteYInstance);
                        }
                    }
                }
            }
        }

        public void Receive(byte[] data, SyncDoc remoteDoc)
        {
            if (!Messages.ContainsKey(remoteDoc))
            {
                Messages[remoteDoc] = new Queue<byte[]>();
            }
            Messages[remoteDoc].Enqueue(data);
        }

        public void Disconnect()
        {
            Messages.Clear();
            Connector.OnlineConnections.Remove(this);
        }
    }
}
