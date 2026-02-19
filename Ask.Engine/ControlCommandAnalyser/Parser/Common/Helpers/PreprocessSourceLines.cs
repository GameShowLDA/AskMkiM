using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  public static class PreprocessSourceLines
  {
    public static string GetClearCommandBody(BaseCommandModel model, List<string> lines)
    {
      List<string> processedLines = CommentsParser.ParseComments(lines, model);

      model.SourceLines = model.SourceLines
          .Where(l => !string.IsNullOrWhiteSpace(l))
          .ToList();

      // Склеиваем всё в одну строку и удаляем \r \n \t
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
