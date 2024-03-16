using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ycs;

namespace Aemo;

public static class YMapExtensions
{
  public static string ToString(this YMap map) => string.Join("\n\t", map.Select(entry => $"{entry.Key}={entry.Value}"));
}