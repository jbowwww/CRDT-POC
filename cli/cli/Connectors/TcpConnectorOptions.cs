using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Aemo.Connectors;

public static class IPEndpointParser
{
  public static IPEndPoint Parse(string endPoint)
  {
    string[] ep = endPoint.Split(':');
    if (ep.Length != 2) throw new FormatException("Invalid endpoint format");
    var hostEntry = Dns.GetHostEntry(ep[0], AddressFamily.InterNetwork);
    var ip = hostEntry.AddressList[0];
    if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out int port))
    {
      throw new FormatException("Invalid port");
    }
    return new IPEndPoint(ip, port);
  }
}

public class TcpConnectorOptions : ConnectorOptions<TcpConnectorOptions>
{
  public bool AutoConnect { get; set; } = true;

  public bool Listen { get; internal set; } = true;

  public IPEndPoint Endpoint { get; internal set; } = IPEndPoint.Parse($"{IPAddress.Any}:2312");

  public IList<IPEndPoint> RemoteEndpoints = new List<IPEndPoint>();

  public string Host => Endpoint.Address.ToString();

  public int Port => Endpoint.Port;

  public TcpConnectorOptions() { }

  public TcpConnectorOptions(string[]? args = null)
   : this()
  {
    if (args != null && args.Length > 0)
      Parse(args);
  }

  public override string ToString()
   => $"[TcpConnectorOptions AutoConnect={AutoConnect} Listen={Listen} Endpoint={Endpoint} RemoteEndpoints={string.Join(", ", RemoteEndpoints.Select(e => e))}";

  /// <summary>
  /// Parse instace type, host and port from string array, presumably/probably taken from a CLI
  /// <summary>
  public override void Parse(string[] args)
  {
    if (args.Length < 2)
      throw new ArgumentException(
          $"Must specify 2| command line arguments! args=[ "
          + string.Join(", ", args.ToList().Select(a => a))
          + "\n\tUsage: {CrdtPoc.ProcessName} [host:port] { [remotehost]:[remoteport] }... ");
    Endpoint = IPEndpointParser.Parse(args[0]);
    for (int i = 1; i < args.Length; i++)
    {
      var remoteEndpoint = IPEndpointParser.Parse(args[i]);
      RemoteEndpoints.Add(remoteEndpoint);
    }
  }

  public override TcpConnectorOptions CopyTo(IConnectorOptions<TcpConnectorOptions> options)
  {
    var tcpOptions = ((TcpConnectorOptions)options);
    tcpOptions.AutoConnect = AutoConnect;
    tcpOptions.Listen = Listen;
    tcpOptions.Endpoint = Endpoint;
    tcpOptions.RemoteEndpoints = RemoteEndpoints;
    return tcpOptions;
  }
}