using System.Text;
using Ycs;

namespace Aemo;

public static class YMapExtensions
{
    public static string ToString(this YMap map)
    {
        StringBuilder sb = new StringBuilder("");//("\n");
        foreach (var entry in map)
        {
            sb.Append($"\t{entry.Key}={entry.Value}");
        }
        return sb.ToString();
    }
}