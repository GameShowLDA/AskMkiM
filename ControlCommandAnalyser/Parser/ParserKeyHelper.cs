using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parser
{
  public static class ParserKeyHelper
  {
    public static HashSet<AlgorithmKey> GetAllowedKeys(ICommandParser parser)
    {
      var attr = parser.GetType()
                       .GetCustomAttributes(typeof(AllowedKeysAttribute), false)
                       .FirstOrDefault() as AllowedKeysAttribute;

      return attr != null
          ? new HashSet<AlgorithmKey>(attr.Keys)
          : new HashSet<AlgorithmKey>();
    }
  }
}
