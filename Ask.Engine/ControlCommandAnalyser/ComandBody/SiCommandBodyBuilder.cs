using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class SiCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is SiCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not SiCommandModel si)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (si != null)
      {
        if (!string.IsNullOrEmpty(si.VoltageSource))
        {
          commandBody.Append($"{si.VoltageSource}, ");
        }
        if (!string.IsNullOrEmpty(si.ResistanceSource) && !string.IsNullOrEmpty(si.ResistanceUnit))
        {
          commandBody.Append($"{si.Resistance}<{si.ResistanceUnit}");
        }
        if (!string.IsNullOrEmpty(si.TimeSource))
        {
          commandBody.Append($", {si.TimeSource}");
        }
        if (si.Scheme != null && si.Scheme.GroupModels.Count > 0)
        {
          commandBody.Append($", ");
        }
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
