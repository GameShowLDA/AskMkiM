using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  public class CuCommandBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is CuCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines)
    {
      if (model is not CuCommandModel cu)
      {
        return newSourseLines;
      }
      var commandBody = new StringBuilder();
      for (int i = 0; i < cu.SourceLines.Count; i++)
      {
        if (cu.SourceLines[i].Contains(cu.CommandNumber))
        {
          cu.SourceLines[i] = cu.SourceLines[i].Replace(cu.CommandNumber, "");
        }
        if (cu.SourceLines[i].Contains(cu.Mnemonic))
        {
          cu.SourceLines[i] = cu.SourceLines[i].Replace(cu.Mnemonic, "");
        }
        commandBody.Append($"{cu.SourceLines[i]}\n");
      }

      newSourseLines.Append(commandBody.ToString());
      return newSourseLines;
    }
  }
}
