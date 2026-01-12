using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class RmCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is RmCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not RmCommandModel rm)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
