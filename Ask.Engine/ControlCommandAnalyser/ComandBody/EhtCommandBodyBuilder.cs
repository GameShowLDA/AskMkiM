using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class EhtCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is EhtCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not EhtCommandModel eht)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (!string.IsNullOrEmpty(eht.LowerLimitResistanceSource))
      {
        commandBody.Append($"{eht.LowerLimitResistance}<");
      }
      if (!string.IsNullOrEmpty(eht.ResistanceUnit))
      {
        commandBody.Append($"{eht.ResistanceUnit}");
      }
      if (!string.IsNullOrEmpty(eht.HigherLimitResistanceSource))
      {
        commandBody.Append($"<{eht.HigherLimitResistance}");
      }
      if (!string.IsNullOrEmpty(eht.TimeSource))
      {
        commandBody.Append($", {eht.TimeSource}");
      }
      if (!string.IsNullOrEmpty(eht.CabelResistanceSource))
      {
        commandBody.Append($", {eht.CabelResistance}");
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
