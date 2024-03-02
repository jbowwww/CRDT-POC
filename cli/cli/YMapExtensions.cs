using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ycs;

namespace Aemo;

public static class YMapExtensions
{
  public static string ToString(this YMap map) => string.Join("\n\t", map.AsEnumerable().Select(entry => $"{entry.Key}={entry.Value}"));
  // {
  //   var sb = new StringBuilder("");//("\n");
  //   foreach (var entry in map)
  //   {
  //     sb.Append($"\n\t{entry.Key}={entry.Value}");
  //   }
  //   return sb.ToString();
  // }
}