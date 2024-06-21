using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cli.Connectors;

public class IPHost
{
    public string HostOrAddress { get; internal init; } = "0.0.0.0";

    public IPHostEntry HostEntry => IsIPAddress ? Dns.GetHostEntry(IPAddress.Parse(HostOrAddress)) : Dns.GetHostEntry(HostOrAddress);

    public string HostName => HostEntry.HostName;

    public IPAddress Address => IsIPAddress ? IPAddress.Parse(HostOrAddress) : HostEntry.AddressList[0];

    public int Port { get; internal init; } = 2221;

    public IPEndPoint EndPoint => new IPEndPoint(Address, Port);

    public bool IsIPAddress => HostOrAddress.All(c => char.IsAsciiDigit(c) || c == '.' || c == ':');

    public bool IsHostName => !IsIPAddress && char.IsAsciiLetter(HostOrAddress[0]);

    public override string ToString() => $"{HostOrAddress}:{Port}";

    // endpoint should be a string like "hostOrIpAddress:port" 
    public static IPHost Parse(string endPoint)
    {
        string[] ep = endPoint.Split(':');
        if (ep.Length != 2) throw new FormatException($"endPoint=\"{endPoint}\" could not be parsed into an IPHost");
        // var hostEntry = Dns.GetHostEntry(ep[0], AddressFamily.InterNetwork);
        // var ip = hostEntry.AddressList[0];
        // var useHostName = !ep[0].All(c => char.IsAsciiDigit(c) || c == '.' || c == ':') && char.IsAsciiLetter(ep[0][0]);
        if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out int port))
        {
            throw new FormatException($"endPoint=\"{endPoint}\" could not be parsed into an IPHost");
        }
        return new IPHost() { HostOrAddress = ep[0], Port = port };
    }

    public static bool TryParse(string endPoint, ref IPHost ipHost)
    {
        try
        {
            ipHost = IPHost.Parse(endPoint);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }
}

public class TcpConnectorOptions : ConnectorOptions<TcpConnectorOptions>
{
    public bool AutoConnect { get; set; } = true;

    public bool Listen { get; internal set; } = true;

    public bool UseHostName { get; set; } = true;

    public IPHost Host { get; internal set; } = new IPHost();

    public IList<IPHost> RemoteHosts = new List<IPHost>();
    
    public TcpConnectorOptions() { }

    public TcpConnectorOptions(string[] args)
    {
        if (args != null && args.Length > 0)
            Parse(args);
    }

    public override string ToString() =>
        $"[TcpConnectorOptions AutoConnect={AutoConnect} Listen={Listen} Host={Host} "
        + $"RemoteHosts={string.Join(", ", RemoteHosts.Select(e => e))}";

    /// <summary>
    /// Parse instace type, host and port from string array, presumably/probably taken from a CLI
    /// <summary>
    public override void Parse(string[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException($"Must specify 2| command line arguments! args=[ {string.Join(", ", args.ToList().Select(a => a))} ]"
                + $"\nUsage: {Process.GetCurrentProcess().ProcessName}" + " <LocalEndpoint> [RemoteEndpoint1] [RemoteEndpoint2] ... [RemoteEndpointN]"
                + "\n\twhere LocalEndpoint and RemoteEndpointN are in the format <hostname_or_ip_address>:<port_number>");
        Host = IPHost.Parse(args[0]);
        for (int i = 1; i < args.Length; i++)
        {
            var remoteHost = IPHost.Parse(args[i]);
            RemoteHosts.Add(remoteHost);
        }
    }
}