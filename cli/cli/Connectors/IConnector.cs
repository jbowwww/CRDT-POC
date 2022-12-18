using System.IO;
namespace Aemo.Connectors;

public interface IConnector
{
    bool IsConnected { get; }
    
    Stream? TxStream { get; }

    Stream? RxStream { get; }
    
    bool Connect();

    void Send(byte[] data, object? transactionOrigin = null);
    // YDocRoot ConnectedDocument { get; init; }
}