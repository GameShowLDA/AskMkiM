using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class NeCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is NeCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not NeCommandModel ne)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (ne.LowerLimitVoltage.HasValue)
      {
        commandBody.Append($"{ne.LowerLimitVoltage}<");
      }
      if (!string.IsNullOrEmpty(ne.VoltageUnit))
      {
        commandBody.Append($"{ne.VoltageUnit}");
      }
      if (ne.HigherLimitVoltage.HasValue)
      {
        commandBody.Append($"<{ne.HigherLimitVoltage}");
      }
      if (!string.IsNullOrEmpty(ne.VoltageSource))
      {
        commandBody.Append($", {ne.VoltageSource}");
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
