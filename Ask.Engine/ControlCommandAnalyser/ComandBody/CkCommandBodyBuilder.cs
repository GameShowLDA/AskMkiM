using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  internal class CkCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is CkCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not CkCommandModel ck)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      if (ck != null)
      {
        if (ck.BusList != null && ck.BusList.Count > 0)
        {
          commandBody.Append($"*");
          for (int i = 0; i < ck.BusList.Count; i++)
          {
            if (ck.BusList.Count == 1 || i == ck.BusList.Count - 1)
            {
              commandBody.Append($"{ck.BusList[i]}");
            }
            else
            {
              commandBody.Append($",{ck.BusList[i]}");
            }
          }
          commandBody.Append($"*");
        }
      }

      return newSourseLines.Append(commandBody.ToString());
    }
  }
}
