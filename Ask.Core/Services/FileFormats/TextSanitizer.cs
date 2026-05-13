using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Services.FileFormats
{
  /// <summary>
  /// Выполняет очистку текста от служебных и устаревших символов.
  /// </summary>
  public static class TextSanitizer
  {
    private static readonly char[] LegacyChars =
    {
      '\u0002',
      '\u0003',
      '\u000E',
      '\u000F',
      '\u000C',
    };

    public static string RemoveLegacyControlChars(string source)
    {
      if (string.IsNullOrEmpty(source))
      {
        return source;
      }

      var result = source;

      foreach (var ch in LegacyChars)
      {
        result = result.Replace(ch.ToString(), string.Empty);
      }

      return result;
    }
  }
}
