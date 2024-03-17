using System.Linq;
using Ycs;

namespace Poc;

public static class YMapExtensions
{
  public static string ToString(this YMap map, string separator = ",") => string.Join(separator, map.Select(entry => $"{entry.Key}={entry.Value}"));
}