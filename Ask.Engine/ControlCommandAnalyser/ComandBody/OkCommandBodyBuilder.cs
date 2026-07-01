using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class OkCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is OkCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not OkCommandModel ok)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (!string.IsNullOrEmpty(ok.ObjectCode))
      {
        commandBody.Append($"{ok.ObjectCode} ");
        if (!string.IsNullOrEmpty(ok.ObjectName))
        {
          commandBody.Append($"* {ok.ObjectName}");
        }
      }
      if (!string.IsNullOrEmpty(ok.TestProgramTableDesignation))
      {
        commandBody.Append($"\n\tОПК={ok.TestProgramTableDesignation} ");
        if (!string.IsNullOrEmpty(ok.LastMeasurment))
        {
          commandBody.Append($"* {ok.ObjectName}");
        }
      }
      if (ok.ControlProgramDocument != null)
      {

        foreach (var kdDoc in ok.ControlProgramDocument)
        {
          if (!string.IsNullOrEmpty(kdDoc))
          {
            commandBody.Append($"\n\tКД={kdDoc} ");
            var match = ok.LastNotificationNumber
              .FindAll(x => x.Item1 == kdDoc);
            for (int i = 0; i < match.Count; i++)
            {
              if (!string.IsNullOrEmpty(match[i].Item2) && i == 0)
              {
                commandBody.Append($"* {match[i].Item2}");
              }
              else if (i > 0)
              {
                commandBody.Append($", {match[i].Item2}");
              }
            }
          }
        }
      }

      if (!string.IsNullOrEmpty(ok.ContolSystemType))
      {
        commandBody.Append($"\n\tИК={ok.ContolSystemType}");
      }
      if (!string.IsNullOrEmpty(ok.OrderNumber))
      {
        commandBody.Append($"\n\tЗАКАЗ={ok.OrderNumber}");
      }
      if (!string.IsNullOrEmpty(ok.DepartmentNumber))
      {
        commandBody.Append($"\n\tЦЕХ={ok.DepartmentNumber}");
      }
      if (ok.Comments != null)
      {
        foreach (var comment in ok.Comments)
        {
          commandBody.Append($"\n\tПРИМ={comment}");
        }
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
