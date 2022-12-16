// ------------------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ycs;

namespace Aemo
{

    public interface IClientProxy
    {
        public void Send(byte[] data, IClientProxy sender);
    }

    public class ClientProxy
    {
        public void Send(byte[] data, IClientProxy sender)
        {
            
        }
    }

    public class DummyLocalClientProxy
    {
        public void Send(byte[] data, IClientProxy sender) { }
    }

    public class YcsConnector
    {
        
        // public ISet<string, IClientProxy> Clients = new HashSet<IClientProxy>();
        public ISet<SyncDoc> Connections = new HashSet<SyncDoc>();
        public ISet<SyncDoc> OnlineConnections = new HashSet<SyncDoc>();
        public Random _prng = new Random();

        public SyncDoc NewDoc(YDocOptions? options = null)
        {
            return new SyncDoc(this, options ?? new YDocOptions() { });
        }

        public static bool FlushNextMessage(SyncDoc sender, SyncDoc receiver)
        {
            var messages = receiver.Messages[sender];
            if (messages.Count == 0)
            {
                receiver.Messages.Remove(sender);
                return false;
            }

            var m = messages.Dequeue();
            // Debug.WriteLine($"MSG {sender.ClientId} -> {receiver.ClientId}, len {m.Length}:");
            // Debug.WriteLine(string.Join(",", m));

            using (var writer = new MemoryStream())
            {
                using (var reader = new MemoryStream(m))
                {
                    YcsProtocolInMemory.ReadSyncMessage(reader, writer, receiver, receiver.Connector);
                }

                if (writer.Length > 0)
                {
                    // Send reply message.
                    var replyMessage = writer.ToArray();
                    // Debug.WriteLine($"REPLY {receiver.ClientId} -> {sender.ClientId}, len {replyMessage.Length}:");
                    // Debug.WriteLine(string.Join(",", replyMessage));
                    sender.Receive(replyMessage, receiver);
                }
            }

            return true;
        }
        
        public bool FlushRandomMessage()
        {
            var conns = OnlineConnections?.Where(conn => conn.Messages.Count > 0).ToList() ?? new List<SyncDoc>();
            if (conns.Count > 0)
            {
                var receiver = conns.ToList()[_prng.Next(0, conns.Count)];
                var keys = receiver.Messages.Keys.ToList();
                var sender = keys[_prng.Next(0, keys.Count)];

                if (!FlushNextMessage(sender, receiver))
                {
                    return FlushRandomMessage();
                }

                return true;
            }

            return false;
        }

        public bool FlushAllMessages()
        {
            var didSomething = false;

            while (FlushRandomMessage())
            {
                didSomething = true;
            }

            return didSomething;
        }

        public void ReconnectAll()
        {
            foreach (var conn in Connections)
            {
                conn.Connect();
            }
        }

        public void DisconnectAll()
        {
            foreach (var conn in Connections)
            {
                conn.Disconnect();
            }
        }

        public void SyncAll()
        {
            ReconnectAll();
            FlushAllMessages();
        }

        public bool DisconnectRandom()
        {
            if (OnlineConnections.Count == 0)
            {
                return false;
            }

            OnlineConnections.ToList()[_prng.Next(0, OnlineConnections.Count)].Disconnect();
            return true;
        }

        public bool ReconnectRandom()
        {
            var reconnectable = new List<SyncDoc>();
            foreach (var conn in Connections)
            {
                if (!OnlineConnections.Contains(conn))
                {
                    reconnectable.Add(conn);
                }
            }

            if (reconnectable.Count == 0)
            {
                return false;
            }

            reconnectable[_prng.Next(0, reconnectable.Count)].Connect();
            return true;
        }
    }
}
