using System.IO;
using System.Net.Sockets;

namespace cli.Connectors;

public interface IConnection
{
  string Id { get; }

  // Socket Socket { get; }

  Stream Stream { get; }

  ConnectionStatus Status { get; }

  void WriteSyncStep1();

  void WriteSyncStep2(byte[] stateVector);

  void ReadSyncStep1();

  void ReadSyncStep2();

  void WriteUpdate(byte[] update);

  void ReadUpdate();

  uint ReadSyncMessage();
}
