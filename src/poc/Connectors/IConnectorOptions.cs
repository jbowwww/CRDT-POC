using System;
using Aemo.Connectors;

namespace Aemo;

public interface IConnectorOptions<TConnector>
  where TConnector : IConnector
{
  IConnectorOptions<TConnector> Parse(string[] args);
}
