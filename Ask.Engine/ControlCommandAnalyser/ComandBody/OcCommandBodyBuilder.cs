using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class OcCommandBodyBuilder
  {
    public bool CanCreate(BaseCommandModel model) => model is OcCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not OcCommandModel oc)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
