namespace cli.Connectors;

public enum ConnectionStatus
{
  Init = 0,
  Connecting,
  Connected,
  Partitioned,
  Disconnecting,
  Disconnected,
  Error
};
