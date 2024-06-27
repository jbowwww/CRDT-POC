using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace cli.Connectors;

public class ServerConnection : Connection
{
    public override string Id { get; init; }

    public ServerConnection(IConnector connector, Socket incomingSocket)
    : base(connector)
    {
        IsServer = true;
        Connect(incomingSocket);
        var remoteIdBuffer = new byte[256];
        var byteCount = Socket.Receive(remoteIdBuffer);
        var length = remoteIdBuffer[0];
        var remoteId = Encoding.ASCII.GetString(remoteIdBuffer, 1, byteCount - 1);
        Id = remoteId;
        _ = Task.Run(() => MessageLoop());
    }
}
