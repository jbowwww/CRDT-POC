using System;
using System.Collections.Generic;
using System.Diagnostics;
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

public class TcpConnectorOptions : ConnectorOptions<TcpConnector, TcpConnectorOptions>
{
  public bool AutoConnect { get; set; } = true;
  public IPEndPoint? ListenEndpoint { get; internal set; } = null;
  public bool IsPrimary { get; internal set; } = false;
  public string? Host => ListenEndpoint?.Address.ToString();
  public int? Port => ListenEndpoint?.Port;
  public bool Listen { get; internal set; } = true;
  public int ListenSocketAcceptQueueSize = 8;
  public IList<IPEndPoint> RemoteEndpoints = new List<IPEndPoint>();
  public int ClientConnectDelay = 2000;
  public int PostConnectDelay = 500;

  public TcpConnectorOptions(string[] args)
  {
    if (args != null && args.Length > 0)
      Parse(args);
  }

  public override string ToString() =>
   $"[TcpConnectorOptions AutoConnect={AutoConnect} Listen={Listen} Endpoint={ListenEndpoint} "
   + $"RemoteEndpoints={string.Join(", ", RemoteEndpoints.Select(e => e))}";

  public override IConnectorOptions<TcpConnector> Parse(string[] args)
  {
    if (args.Length < 2)
    {
      throw new ArgumentException($"Must specify 2| command line arguments! args=[ {string.Join(", ", args.ToList().Select(a => a))} ]"
          + $"\nUsage: {Process.GetCurrentProcess().ProcessName}" + " <LocalEndpoint> [RemoteEndpoint1] [RemoteEndpoint2] ... [RemoteEndpointN]"
          + "\n\twhere LocalEndpoint and RemoteEndpointN are in the format <hostname_or_ip_address>:<port_number>");
    }
    foreach (var arg in args)
    {
      if (arg.StartsWith("-"))
      {
        var param = arg[1..];
        if (param == "P")
          IsPrimary = true;
      }
      else
      {
        if (ListenEndpoint == null)
        {
          ListenEndpoint = IPEndpointParser.Parse(arg);
        }
        else
        {
          RemoteEndpoints.Add(IPEndpointParser.Parse(arg));
        }
      }
    }
    return this;
  }
}