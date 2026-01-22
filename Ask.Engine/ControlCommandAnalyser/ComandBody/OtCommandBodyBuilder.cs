using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class OtCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is OtCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not OtCommandModel ot)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (ot != null)
      {
        if (!string.IsNullOrEmpty(ot.TimeSource))
        {
          commandBody.Append($"{ot.TimeSource}  ");
        }
        if (ot.BusPointsDictionary != null && ot.BusPointsDictionary.Count > 0)
        {
          foreach (var busPoint in ot.BusPointsDictionary)
          {
            commandBody.Append($"*");
            commandBody.Append($"{busPoint.Key}:");
            for (int i = 0; i < busPoint.Value.Count; i++)
            {
              if (busPoint.Value.Count == 1 || i == busPoint.Value.Count - 1)
              {
                commandBody.Append($"{busPoint.Value[i].Item1}");
              }
              else
              {
                commandBody.Append($",{busPoint.Value[i].Item1}");
              }
            }
          }
          commandBody.Append($"*");
        }
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
