using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;
using System.Text.RegularExpressions;

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
        var sourceLine = cu.SourceLines[i];
        if (i == 0)
        {
          sourceLine = StripCommandHeader(sourceLine, cu.CommandNumber, cu.Mnemonic);
        }

        commandBody.Append($"{sourceLine}\n");
      }

      newSourseLines.Append(commandBody.ToString());
      return newSourseLines;
    }

    private static string StripCommandHeader(string line, string commandNumber, string mnemonic)
    {
      if (string.IsNullOrEmpty(line))
      {
        return string.Empty;
      }

      var pattern = $@"^\s*{Regex.Escape(commandNumber)}\s+{Regex.Escape(mnemonic)}(?=\s|$)";
      return Regex.Replace(line, pattern, string.Empty, RegexOptions.IgnoreCase);
    }
  }
}
