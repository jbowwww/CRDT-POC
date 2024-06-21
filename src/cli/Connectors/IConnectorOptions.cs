namespace cli.Connectors;

public interface IConnectorOptions<TConnector>
   where TConnector : ConnectorOptions<TConnector>, new()
{
    void Parse(string[] args);
}
