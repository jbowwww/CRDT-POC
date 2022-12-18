using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Aemo.Connectors;

public class TcpConnector : IConnector
{
    public IPAddress IP;

    public TcpConnector(string connectionString)
    {
        // TODO: Take an IPv4 address in connection string & a port number & connect a TCP socket
        // Note: You need a listener/server and a client, how to model/handle that??
        // TODO: Write a (de)multiplexer IConnector class(es) that probably use a MemoryStream/BufferedStream
        // to write to multiple TcpConnectors. ALso opposite for reading.
        // TODO: Then you either need one global parent/root doc containing all shared docs
        // and then you can read from connector's rxsocket and always update that global Root
        // OR maintain a map of constructed YDoc/ConnectedDocument's and write the ID
        // I think the 1st option is best there, because the doc structure implements subdoc maps already so use it
        // But I read somewhere there may be situations that are suited to the 2nd option (where did i read..?)
    }

    public bool IsConnected { get; protected set; }

    public Stream? TxStream { get; protected set; }

    public Stream? RxStream { get; protected set; }

    public bool Connect()
    {
        throw new NotImplementedException();
    }

    public void Send(byte[] data, object? transactionOrigin = null)
    {
        throw new NotImplementedException();
    }
}
