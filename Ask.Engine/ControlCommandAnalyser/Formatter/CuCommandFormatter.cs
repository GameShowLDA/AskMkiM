using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  /// <summary>
  /// Форматтер для команды ЦУ (сообщение оператору).
  /// </summary>
  public class CuCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is CuCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not CuCommandModel cu)
        yield break;

      var firstLine = $"{cu.CommandNumber} {cu.Mnemonic}" + (cu.IsDocument ? " Д" : "");
      yield return firstLine;

      // Ключи
      if (cu.AlgorithmKey.Count > 0)
        yield return $"\tКлючи команды: {string.Join(", ", cu.AlgorithmKey)}";

      yield return $"\tТип сообщения: {cu.CuType}";
      yield return $"\tТекст сообщения:";
      foreach (var line in cu.MessageText.Split('\n', '\r'))
      {
        var trimmed = line.Trim();
        if (!string.IsNullOrEmpty(trimmed))
          yield return $"\t\t{trimmed}";
      }
      if (cu.Comment.Count > 0)
      {
        yield return $"\tКомментарии:";
        foreach (var line in cu.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }

      yield return string.Empty;
    }
  }
}
