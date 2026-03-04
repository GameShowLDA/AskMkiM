using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class PrCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is PrCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not PrCommandModel pr)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (!string.IsNullOrEmpty(pr.ConnectedLowerLimitResistanceSource))
      {
        commandBody.Append($"{pr.ConnectedLowerLimitResistance}<");
      }
      if (!string.IsNullOrEmpty(pr.ResistanceUnit))
      {
        commandBody.Append($"{pr.ResistanceUnit}");
      }
      if (!string.IsNullOrEmpty(pr.ConnectedHigherLimitResistanceSource))
      {
        commandBody.Append($"<{pr.ConnectedHigherLimitResistance}");
      }
      if (!string.IsNullOrEmpty(pr.TimeSource))
      {
        commandBody.Append($", {pr.TimeSource}");
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
