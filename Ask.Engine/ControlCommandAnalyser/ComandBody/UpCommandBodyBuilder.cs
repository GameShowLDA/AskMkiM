using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class UpCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is UpCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not UpCommandModel up)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (!string.IsNullOrEmpty(up.TargetLabel))
      {
        commandBody.Append($"{up.TargetLabel}");
      }
      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
