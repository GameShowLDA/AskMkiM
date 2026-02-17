using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class VshCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is VshCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not VshCommandModel vsh)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (vsh.BusStructure.Count > 0)
      {
        var busNumber = string.Empty;
        commandBody.Append($"*");
        foreach (var item in vsh.BusStructure)
        {
          busNumber = item.Key.GetDescription();

          foreach (var number in item.Value)
          {
            commandBody.Append($"{busNumber}Ш:{number}*");
          }
        }
      }
      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
