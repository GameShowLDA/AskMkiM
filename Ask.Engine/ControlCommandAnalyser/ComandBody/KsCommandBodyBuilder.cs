using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class KsCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is KsCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not KsCommandModel ks)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (ks.LowerLimitResistance != null && ks.ResistanceUnit != null)
      {
        commandBody.Append($"{ks.LowerLimitResistance}<{ks.ResistanceUnit}");
      }
      if (ks.HigherLimitResistance != null)
      {
        commandBody.Append($"<{ks.HigherLimitResistance} ");
      }


      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
