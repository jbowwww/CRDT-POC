using System;

namespace Aemo;

public interface IConnectorOptions<T>
   where T : ConnectorOptions<T>, new()
{
  void Parse(string[] args);

  T CopyTo(ConnectorOptions<T> options);
}
