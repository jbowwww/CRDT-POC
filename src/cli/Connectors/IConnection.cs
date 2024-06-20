using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace cli.Connectors;

public interface IConnection
{
  string Id { get; }

  EndPoint LocalEndpoint { get; }

  EndPoint RemoteEndpoint { get; }

  // Socket Socket { get; }

  bool IsServer { get; init; }

  Stream Stream { get; }

  bool Synced { get; }

  ConnectionStatus? ConnectionStatus { get; }
  ConnectionStatus Status { get; }

  Exception? Error { get; }

  void WriteSyncStep1();

  void WriteSyncStep2(byte[] stateVector);

  void ReadSyncStep1();

  void ReadSyncStep2();

  void WriteUpdate(byte[] update);

  void ReadUpdate();

  uint ReadSyncMessage();
}
