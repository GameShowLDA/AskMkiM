using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Formatter
{
  public class RmCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is RmCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is RmCommandModel rm)
      {
        yield return $"{rm.CommandNumber} {rm.Mnemonic}";

        foreach (var pair in rm.PointsMap)
          yield return $"\t{pair.Key} => {pair.Value}";

        yield return string.Empty;
      }
      else
      {
        yield break;
      }
    }
  }
}
