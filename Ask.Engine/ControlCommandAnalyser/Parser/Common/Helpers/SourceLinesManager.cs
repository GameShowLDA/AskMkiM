using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Менеджер проверки исходных строк команды.
  /// Проверяет наличие строк и корректность отступов.
  /// </summary>
  public class SourceLinesManager
  {
    /// <summary>
    /// Выполняет комплексную проверку строк команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="lines">Строки тела команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <returns>
    /// <c>true</c>, если строки валидны; иначе <c>false</c>.
    /// </returns>
    public static bool Check(BaseCommandModel model, List<string> lines, int numberLine)
    {
      if (LinesExist(model, lines, numberLine) == true)
      {
        NormalizeIndentation(model, lines);
        return IndentationCheck(model, lines, numberLine);
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Проверяет наличие строк тела команды.
    /// </summary>
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

    /// <summary>
    /// Проверяет корректность отступов в строках.
    /// </summary>
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

    /// <summary>
    /// Автоматически нормализует отступы:
    /// убирает отступ перед заголовком команды и добавляет отступ у строк продолжения.
    /// </summary>
    private static void NormalizeIndentation(BaseCommandModel model, List<string> lines)
    {
      var expectedStart = $"{model.CommandNumber} {model.Mnemonic}";

      for (int i = 0; i < lines.Count; i++)
      {
        var line = lines[i];
        if (string.IsNullOrWhiteSpace(line))
          continue;

        var trimmed = line.TrimStart();
        if (trimmed.StartsWith(expectedStart))
        {
          lines[i] = trimmed;
          continue;
        }

        if (i > 0 && !char.IsWhiteSpace(line[0]) && line.Trim() != "*")
        {
          lines[i] = $" {line}";
        }
      }

      model.SourceLines = new List<string>(lines);
    }
  }
}
