using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using cli.Options;

namespace cli.Connectors;

public class TcpConnectorOptions : Options<TcpConnectorOptions>
{

    public bool Listen { get; internal set; } = true;

    public int ListenSocketAcceptQueueSize = 8;

    public bool UseHostName { get; set; } = true;

    // [Option<IPHost>(Name = "yeh", Parser = typeof(IPHost))]
    public IPHost Host { get; internal set; } = null!;
    
    public bool AutoConnect { get; set; } = true;

    public IList<IPHost> RemoteHosts = new List<IPHost>();

    public override string ToString() =>
        $"[{GetType().Name} AutoConnect={AutoConnect} Listen={Listen} Host={Host} "
        + $"RemoteHosts={string.Join(", ", RemoteHosts.Select(e => e))}";

    /// <summary>
    /// Parse instace type, host and port from string array, presumably/probably taken from a CLI
    /// <summary>
    public override TcpConnectorOptions Parse(string[] args)
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

        return this;
    }

    static IPHost IOptionParser<IPHost>.Parse(string option) => IPHost.Parse(option);
}
