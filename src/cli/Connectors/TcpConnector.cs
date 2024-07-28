using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using cli.Options;

namespace cli.Connectors;

public class TcpConnector : Connector<TcpConnectorOptions>
{
    private readonly object _syncObject = new();

    public const int ListenSocketAcceptQueueSize = 8;

    public const int ClientConnectDelay = 5000;

    public override string Id => Options.Host.HostOrAddress;

    public TcpConnector()
    {
        Connections.Change += async (sender, e) =>
        {
            if (e.Change == ConnectionDictionary<IConnection>.ChangeType.Remove)
            {
                var endpoint = e.Connection.RemoteEndpoint.ToString();
                if (endpoint == null)
                {
                    throw new ApplicationException($"TcpConnector.Connections: Connection {e.Connection} removed with error {e.Connection.Error}", e.Connection.Error);
                }
                await Task.Delay(1000).ContinueWith(task =>
            {
                if (Status < ConnectionStatus.Disconnecting)
                {
                    ClientConnect(Options.RemoteHosts.First(rh => rh.EndPoint.Address.Equals(IPEndPoint.Parse(endpoint).Address)).EndPoint);
                }
            });
            }
        };
    }

    public override async Task Connect()
    {
        lock (_syncObject)
        {
            if (!IsInit)
            {
                Console.WriteLine($"TcpConnector.Connect(): Aborting on connector.Id={Id} because Status={Status}");
                return;
            }
            Status = ConnectionStatus.Connecting;
        }
        ServerListen();
        await Task.Delay(ClientConnectDelay);
        // lock (_syncObject)
        // {
        foreach (var remoteHost in Options.RemoteHosts)
        {
            ClientConnect(remoteHost.EndPoint);
        }
        Status = ConnectionStatus.Connected;
        // }
        await base.Connect();
    }

    public override void Disconnect()
    {
        Console.Write($"Disconnect(): Disconnecting client from connector.Id={Id} ... ");
        lock (_syncObject)
        {
            if (IsConnected)
            {
                Status = ConnectionStatus.Disconnecting;
            }
        }
        Console.WriteLine("OK");//$"Disconnect(): End Disconnect() client with connector.Id={Id} ...");
        base.Disconnect();
    }

    public async void ServerListen()
    {
        var listenSocket = new Socket(Options.Host.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            Console.Write($"ServerListen(): Listening on {Options.Host.EndPoint/* listenSocket.LocalEndPoint */} ListenSocketAcceptQueueSize={ListenSocketAcceptQueueSize} ... ");
            listenSocket.Bind(Options.Host.EndPoint); //new IPEndPoint(Dns.g(Options.Host.HostOrAddress, Options.Host.Address.AddressFamily)).);//Options.Host.Address, Options.Host.Port));
            listenSocket.Listen(ListenSocketAcceptQueueSize);
            Console.WriteLine("OK");
            await Task.Run(async () =>
            {
                while (Options.Listen && IsConnected)
                {
                    var acceptedSocket = await listenSocket.AcceptAsync();
                    Console.Write($"ServerListen(): Accepting connection from [{acceptedSocket.RemoteEndPoint}->{acceptedSocket.LocalEndPoint}] ... ");
                    lock (_syncObject)
                    {
                        if (acceptedSocket.RemoteEndPoint is IPEndPoint remoteIP)
                        {
                            var remoteIdBuffer = new byte[256];
                            var byteCount = acceptedSocket.Receive(remoteIdBuffer);
                            var length = remoteIdBuffer[0];
                            var remoteId = Encoding.ASCII.GetString(remoteIdBuffer, 1, byteCount - 1);
                            if (Connections.ContainsKey(remoteId))
                            {
                                Console.Write($"Closing: Connection {remoteId} (byteCount={byteCount}) already exists: Connection=${Connections[remoteId]}");
                                acceptedSocket.Shutdown(SocketShutdown.Both);
                                acceptedSocket.Close();
                                Console.WriteLine("OK");
                            }
                            else
                            {
                                Console.WriteLine($"OK (byteCount={byteCount} length={length} remoteId={remoteId})");
                                var connection = new Connection(remoteId, this, acceptedSocket, true);
                            }
                        }
                    }
                }
                lock (_syncObject)
                {
                    Console.Write($"ServerListen(): Closing the listener on Endpoint={Options.Host.EndPoint} ...");
                    listenSocket.Close(1000);
                    while (Connections.Count > 0)
                        ;
                    if (Status == ConnectionStatus.Disconnecting)
                    {
                        Status = ConnectionStatus.Disconnected;
                    }
                    Console.Write("OK");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ServerListen(): Exception: Options.Listen={Options.Listen} Status={Status}\n\t{ex}");
            Status = ConnectionStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
        }
    }

    public void ClientConnect(IPEndPoint remoteEndpoint)
    {
        lock (_syncObject)
        {
            if (!Connections.ContainsKey(remoteEndpoint.ToString()))
            {
                var client = new TcpClient(remoteEndpoint.AddressFamily);
                Console.Write($"ClientConnect(): Connecting to {remoteEndpoint} ... ");
                client.Connect(remoteEndpoint);
                Console.WriteLine($"OK");
                var connection = new Connection(remoteEndpoint.ToString(), this, client.Client);
                byte[] thisIdBuffer = Encoding.ASCII.GetBytes(Options.Host.EndPoint.ToString()).Prepend((byte)Id.Length).ToArray();
                //(byte[])new byte[1] { (byte)Id.Length }.Concat();
                var byteCount = client.Client.Send(thisIdBuffer);
                Console.WriteLine($"(Id={Id} thisIdBuffer={thisIdBuffer} byteCount={byteCount})");//$"ClientConnect(): Established connection={connection}");
            }
            else
            {
                var connection = Connections[remoteEndpoint.ToString()];
                Console.WriteLine($"ClientConnect(): Already have server connection to {remoteEndpoint}: {connection}");
            }
        }
    }
}
