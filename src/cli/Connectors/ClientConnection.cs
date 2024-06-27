
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using cli.Connectors;

public class ClientConnection : Connection
{
    public override string Id { get; init; }

    public ClientConnection(IConnector connector, IPEndPoint remoteEndpoint)
    : base(connector)
    {
        IsServer = false;
        Id = remoteEndpoint.ToString();
        Connector = connector;
        var client = new TcpClient(remoteEndpoint.AddressFamily);
        client.Connect(remoteEndpoint);
        Connect(client.Client);
        byte[] thisIdBuffer = Encoding.ASCII.GetBytes(connector.Id).Prepend((byte)connector.Id.Length).ToArray();
        //(byte[])new byte[1] { (byte)Id.Length }.Concat();
        var byteCount = client.Client.Send(thisIdBuffer);
    }
}
