using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace cli.Connectors;

public class TcpConnector : Connector<TcpConnectorOptions>
{
    private readonly object _syncObject = new();

    public const int ClientConnectDelay = 3000;

    public override string Id => Options.Host.HostOrAddress + ":" + Options.Host.Port;//.EndPoint.ToString();// Options.Host?.Address + ":" + Options.Host?.Port;

    public TcpConnector()
    {
        // Connections.Change += async (sender, e) =>
        // {
        //     if (e.Change == ConnectionDictionary<IConnection>.ChangeType.Remove
        //      && Status < ConnectionStatus.Disconnecting)
        //     {
        //         var endpoint = e.Connection.RemoteEndpoint.ToString();
        //         if (endpoint == null)
        //         {
        //             throw new ApplicationException($"TcpConnector.Connections: Connection {e.Connection} removed with error {e.Connection.Error}", e.Connection.Error);
        //         }
        //         await Task.Delay(1000).ContinueWith(task =>
        //     {
        //         ClientConnect(Options.RemoteHosts.First(rh => rh.EndPoint.Address.Equals(IPEndPoint.Parse(endpoint).Address)).EndPoint);
        //     });
        //     }
        // };
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
        Console.WriteLine(ToString($"Connect(): Options={Options}"));
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
    }

    public override void Disconnect()
    {
        if (IsConnected)
        {
            lock (_syncObject)
            {
                Console.Write(ToString($"Disconnect() ... "));
                Status = ConnectionStatus.Disconnecting;
                while (Connections.Count > 0)
                    ;
                if (Status == ConnectionStatus.Disconnecting)
                {
                    Status = ConnectionStatus.Disconnected;
                }
                Console.WriteLine("OK");
            }
        }
    }

    public async void ServerListen()
    {
        var listenSocket = new Socket(Options.Host.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            listenSocket.Bind(Options.Host.EndPoint);
            listenSocket.Listen(Options.ListenSocketAcceptQueueSize);
            await Task.Run(async () =>
            {
                while (Options.Listen && IsConnected)
                {
                    var acceptedSocket = await listenSocket.AcceptAsync();
                    lock (_syncObject)
                    {
                        var acceptedRemoteEndpoint = acceptedSocket.RemoteEndPoint as IPEndPoint;
                        var connectionId = Options.RemoteHosts.First(host =>
                            host.Address.ToString() == acceptedRemoteEndpoint?.Address.ToString()
                        ).EndPoint.ToString();
                        if (!Connections.ContainsKey(connectionId))
                        {
                            Console.Write(ToString($"ServerListen(): Accepting connection from [{acceptedSocket.RemoteEndPoint}->{acceptedSocket.LocalEndPoint}] ... "));
                            var connection = new ServerConnection(this, acceptedSocket);
                            Connections.Add(connection);
                            Console.WriteLine("OK");
                        }
                        else
                        {
                            Console.WriteLine(ToString($"ServerListen(): Rejecting connection with Id={connectionId} already exists: ${Connections[connectionId]}"));
                            acceptedSocket.Close(ClientConnectDelay);
                            Console.WriteLine("OK");
                        }
                    }
                }
                lock (_syncObject)
                {
                    Console.Write(ToString($"ServerListen(): Closing the listener on Endpoint={Options.Host.EndPoint} ..."));
                    listenSocket.Close(1000);
                    while (Connections.Count > 0)
                        ;
                    if (Status == ConnectionStatus.Disconnecting)
                    {
                        Status = ConnectionStatus.Disconnected;
                    }
                    Console.WriteLine("OK");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ServerListen(): Exception: {ex}");
            Disconnect();
            Status = ConnectionStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
        }
    }

    void ClientConnect(IPEndPoint remoteEndpoint)
    {
        var connectionId = remoteEndpoint.ToString();
        lock (_syncObject)
        {
            if (!Connections.ContainsKey(connectionId))
            {
                try
                {
                    var connection = new ClientConnection(this, remoteEndpoint);
                    Connections.Add(connection);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                    Disconnect();
                    Status = ConnectionStatus.Error;  // TODO: I think this status needs to be per IConnection not IConnector? at least per server/client
                }
            }
        }
    }
}
