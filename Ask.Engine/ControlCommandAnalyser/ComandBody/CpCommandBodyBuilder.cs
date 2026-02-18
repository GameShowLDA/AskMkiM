using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class CpCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is CpCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not CpCommandModel cp)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      for (int i = 0; i < cp.SourceLines.Count; i++)
      {
        if (cp.SourceLines[i].Contains(cp.CommandNumber))
        {
          cp.SourceLines[i] = cp.SourceLines[i].Replace(cp.CommandNumber, "");
        }
        if (cp.SourceLines[i].Contains(cp.Mnemonic))
        {
          cp.SourceLines[i] = cp.SourceLines[i].Replace(cp.Mnemonic, "");
        }
        commandBody.Append($"{cp.SourceLines[i]}\n");
      }

      newSourseLines.Append(commandBody.ToString());
      return newSourseLines;
    }
  }
}
