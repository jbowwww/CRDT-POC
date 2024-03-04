// ------------------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
// ------------------------------------------------------------------------------

using System;
using System.IO;

namespace Ycs
{
    public class SyncProtocol
    {
        public const uint MessageYjsSyncStep1 = 0;
        public const uint MessageYjsSyncStep2 = 1;
        public const uint MessageYjsUpdate = 2;

        public static string EncodeBytes(byte[] arr) => Convert.ToBase64String(arr);
        public static byte[] DecodeString(string str) => Convert.FromBase64String(str);

        public static void WriteSyncStep1(Stream stream, YDoc doc)
        {
            stream.WriteVarUint(MessageYjsSyncStep1);
            var sv = doc.EncodeStateVectorV2();
            stream.WriteVarUint8Array(sv);
        }

        public static void WriteSyncStep2(Stream stream, YDoc doc, byte[] encodedStateVector)
        {
            stream.WriteVarUint(MessageYjsSyncStep2);
            var update = doc.EncodeStateAsUpdateV2(encodedStateVector);
            stream.WriteVarUint8Array(update);
        }

        public static void ReadSyncStep1(Stream stream, YDoc doc)
        {
            var encodedStateVector = stream.ReadVarUint8Array();
            WriteSyncStep2(stream, doc, encodedStateVector);
        }

        public static void ReadSyncStep2(Stream stream, YDoc doc, object transactionOrigin)
        {
            var update = stream.ReadVarUint8Array();
            doc.ApplyUpdateV2(update, transactionOrigin);
        }

        public static void WriteUpdate(Stream stream, byte[] update)
        {
            stream.WriteVarUint(MessageYjsUpdate);
            stream.WriteVarUint8Array(update);
        }

        public static void ReadUpdate(Stream stream, YDoc doc, object transactionOrigin)
        {
            ReadSyncStep2(stream, doc, transactionOrigin);
        }


        // I modified some of the internal Ycs code to suit myself, adding back this override
        // at least means I don't have to bother changing TestConnector (not that I actually use it anyway)
        // What I'm actually glossing over is that originally this func took a separate resader and writer,
        // I am just using the same network stream for both and don't seem to have any dramas so far.
        public static uint ReadSyncMessage(Stream stream, Stream _, YDoc doc, object transactionOrigin) =>
            ReadSyncMessage(stream, doc, transactionOrigin);

        public static uint ReadSyncMessage(Stream stream, YDoc doc, object transactionOrigin)
        {
            var messageType = stream.ReadVarUint();

            switch (messageType)
            {
                case MessageYjsSyncStep1:
                    ReadSyncStep1(stream, doc);
                    break;
                case MessageYjsSyncStep2:
                    ReadSyncStep2(stream, doc, transactionOrigin);
                    break;
                case MessageYjsUpdate:
                    ReadUpdate(stream, doc, transactionOrigin);
                    break;
                default:
                    throw new Exception($"Unknown message type: {messageType}");
            }

            return messageType;
        }
    }
}
