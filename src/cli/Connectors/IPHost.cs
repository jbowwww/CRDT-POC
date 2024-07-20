using System;
using System.Globalization;
using System.Linq;
using System.Net;

namespace cli.Options;

public class IPHost
{
    public string HostOrAddress { get; internal init; } = "0.0.0.0";

    public IPHostEntry HostEntry => /* IsIPAddress ? Dns.GetHostEntry(IPAddress.Parse(HostOrAddress)) : */ Dns.GetHostEntry(HostOrAddress);

    public string HostName => HostEntry.HostName;

    public IPAddress Address => IsIPAddress ? IPAddress.Parse(HostOrAddress) : HostEntry.AddressList[0];

    public int Port { get; internal init; } = 2221;

    public IPEndPoint EndPoint => new IPEndPoint(Address, Port);

    public bool IsIPAddress => HostOrAddress.All(c => char.IsAsciiDigit(c) || c == '.' || c == ':');

    public bool IsHostName => !IsIPAddress && char.IsAsciiLetter(HostOrAddress[0]);

    public override string ToString() => $"{HostOrAddress}:{Port}";

    // endpoint should be a string like "hostOrIpAddress:port" 
    public IPHost(string endPoint)
    {
        string[] ep = endPoint.Split(':');
        if (ep.Length != 2) throw new FormatException($"endPoint=\"{endPoint}\" could not be parsed into an IPHost");
        // var hostEntry = Dns.GetHostEntry(ep[0], AddressFamily.InterNetwork);
        // var ip = hostEntry.AddressList[0];
        // var useHostName = !ep[0].All(c => char.IsAsciiDigit(c) || c == '.' || c == ':') && char.IsAsciiLetter(ep[0][0]);
        if(!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out int port))
        {
            throw new FormatException($"endPoint=\"{endPoint}\" could not be parsed into an IPHost");
        }
        HostOrAddress = ep[0]; 
        Port = port;
    }

    public static IPHost Parse(string endPoint)
    {
        return new IPHost(endPoint);
    }

    public static bool TryParse(string endPoint, out IPHost? ipHost)
    {
        try
        {
            ipHost = new IPHost(endPoint);
            return true;
        }
        catch (FormatException)
        {
            ipHost = null;
            return false;
        }
    }
}
