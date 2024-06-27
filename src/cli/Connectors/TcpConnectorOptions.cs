using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace cli.Connectors;

public class TcpConnectorOptions : ConnectorOptions<TcpConnectorOptions>
{
    public bool AutoConnect { get; set; } = true;

    public bool Listen { get; internal set; } = true;

    public bool UseHostName { get; set; } = true;

    public IPHost Host { get; internal set; } = null!; //new IPHost();

    public IList<IPHost> RemoteHosts = new List<IPHost>();

    public int ListenSocketAcceptQueueSize = 8;

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
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Length == 0) continue;
            if (arg.First() == '-')
            {
                // TODO: Short options
            }
            else if (Host == null)
            {
                Host = IPHost.Parse(arg);
            }
            else
            {
                RemoteHosts.Add(IPHost.Parse(arg));
            }
        }
    }
}