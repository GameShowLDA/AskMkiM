using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  public class CheckSourceLines
  {
    public static bool ManageCheck(BaseCommandModel model, List<string> lines, int numberLine)
    {
      if (LinesExist(model, lines, numberLine) == true)
      {
        return IndentationCheck(model, lines, numberLine);
      }
      else
      {
        return false;
      }
    }

    private static bool LinesExist(BaseCommandModel model, List<string> lines, int numberLine)
    {
      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {model.CommandNumber} {model.Mnemonic} (строка {numberLine})");
        model.Errors.Add(EhtErrors.EmptyCommandBody(numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
        return false;
      }
      return true;
    }
    private static bool IndentationCheck(BaseCommandModel model, List<string> lines, int numberLine)
    {
      var errors = IndentationCheker.CheckIndentationErrors(lines, model.CommandNumber, model.Mnemonic);
      if (errors.Count > 0)
      {
        foreach (var error in errors)
        {
          LogError(error);
          model.Errors.Add(GeneralErrors.IndentationError(model.Mnemonic, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
        }
          return false;
      }
      return true;
    }
  }
}
