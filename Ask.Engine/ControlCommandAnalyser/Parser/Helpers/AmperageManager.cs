using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  internal class AmperageManager
  {
    public static (double? value, string? unit) Parse(string raw, string unit)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return (null, unit);

      return (CommonParameterParser.ParseToDouble(raw), unit);
    }
  }
}
