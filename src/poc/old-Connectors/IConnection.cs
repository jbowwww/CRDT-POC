using System.IO;

namespace Aemo.Connectors;

public interface IConnection
{
  public string Id { get; }

  public Stream Stream { get; }

  public ConnectionStatus Status { get; }

  void WriteSyncStep1();

  void WriteSyncStep2(byte[] stateVector);

  void ReadSyncStep1();

  void ReadSyncStep2();

  void WriteUpdate(byte[] update);

  void ReadUpdate();

  uint ReadSyncMessage();
}
