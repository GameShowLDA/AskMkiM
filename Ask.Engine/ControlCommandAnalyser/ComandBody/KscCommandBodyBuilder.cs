using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class KscCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is KscCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not KscCommandModel ksc)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
