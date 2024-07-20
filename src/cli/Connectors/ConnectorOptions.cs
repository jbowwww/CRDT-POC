using System;
using Ycs;

public class ConnectorOptions
{
    public static ConnectorOptions Default = new ConnectorOptions();
    
    public string RootDocumentName { get; init; } = "ConnectedRoot";

    public YDoc Document { get; internal set; } = null!;

    public virtual string Usage { get; init; } = $"\nUsage: {System.Diagnostics.Process.GetCurrentProcess().ProcessName}";

    public ConnectorOptions() { }

    public virtual void Parse(string[] args) { }

    public bool TryParse(string[] args)
    {
        try
        {
            Parse(args);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(Usage);
            return false;
        }
    }
}
