using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Менеджер предварительной обработки исходных строк команды.
  /// Удаляет комментарии, нормализует и формирует тело команды.
  /// </summary>
  public static class PreprocessSourceLines
  {
    /// <summary>
    /// Возвращает нормализованное тело команды в виде одной строки
    /// без переводов строк, табуляции и пустых строк.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Строка с очищенным телом команды.</returns>
    public static string GetClearCommandBody(BaseCommandModel model, List<string> lines)
    {
      List<string> processedLines = CommentsParser.ParseComments(lines, model);

      model.SourceLines = model.SourceLines
          .Where(l => !string.IsNullOrWhiteSpace(l))
          .ToList();

      var body = string.Concat(processedLines.Count > 0 && processedLines.FindAll(l => string.IsNullOrEmpty(l) || string.IsNullOrWhiteSpace(l)).Count == 0 ?
        processedLines : model.SourceLines)
        .Replace("\r", "")
        .Replace("\n", "")
        .Replace("\t", "");

      LogDebug($"Нормализованное тело команды (в одну строку): \"{body}\"");
      return body;
    }
  }
}
