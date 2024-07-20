using System;
using System.Collections.Generic;
using System.Linq;

namespace cli.Options;

public class TcpConnectorOptions : ConnectorOptions, IOptionsParser<TcpConnectorOptions>
{
    public bool Listen { get; internal set; } = true;

    public int ListenSocketAcceptQueueSize = 8;

    public bool UseHostName { get; set; } = true;

    [Option(IsPositional = true)]
    public IPHost Host { get; internal set; } = null!;
    
    public bool AutoConnect { get; set; } = true;

    [Option(IsPositional = true, IsList = true)] 
    public IList<IPHost> RemoteHosts = new List<IPHost>();

    public override string Usage =>
        $"{base.Usage} <LocalEndpoint> [RemoteEndpoint1] [RemoteEndpoint2] ... [RemoteEndpointN]" +
        "\n\twhere LocalEndpoint and RemoteEndpointN are in the format <hostname_or_ip_address>:<port_number>";

    public override string ToString() =>
        $"[{GetType().Name} AutoConnect={AutoConnect} Listen={Listen} Host={Host} " +
        $"RemoteHosts={string.Join(", ", RemoteHosts.Select(e => e))}";

    /// <summary>
    /// Parse instace type, host and port from string array, presumably/probably taken from a CLI
    /// <summary>
    public override void Parse(string[] args)
    {
        if (args.Length < 2)
        {
            throw new ArgumentException($"Must specify 2| command line arguments!");
        }

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Length == 0) continue;
            if (arg.StartsWith("--"))
            {
                // TODO: Long-named options
            }
            if (arg.First() == '-')
            {
                // TODO: Short-named options
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

    public TcpConnectorOptions Parse(string option)
    {
        throw new NotImplementedException();
    }
}
