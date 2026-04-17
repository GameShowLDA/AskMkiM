using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

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
