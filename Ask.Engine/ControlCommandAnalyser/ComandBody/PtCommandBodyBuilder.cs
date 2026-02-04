using Ask.Engine.ControlCommandAnalyser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class PtCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is PtCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not PtCommandModel pt)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (pt != null)
      {
        if (!string.IsNullOrEmpty(pt.TimeSource))
        {
          commandBody.Append($"{pt.TimeSource}  ");
        }
        if (pt.BusPointsDictionary != null && pt.BusPointsDictionary.Count > 0)
        {
          foreach (var busPoint in pt.BusPointsDictionary)
          {
            commandBody.Append($"*");
            commandBody.Append($"{busPoint.Key}:");
            for (int i = 0; i < busPoint.Value.Count; i++)
            {
              if (busPoint.Value.Count == 1 || i == busPoint.Value.Count - 1)
              {
                commandBody.Append($"{busPoint.Value[i].Mnemonic}");
              }
              else
              {
                commandBody.Append($",{busPoint.Value[i].Mnemonic}");
              }
            }
          }
          commandBody.Append($"*");
        }
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
