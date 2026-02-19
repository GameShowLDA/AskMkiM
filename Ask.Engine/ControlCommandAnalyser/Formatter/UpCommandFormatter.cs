using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  /// <summary>
  /// Форматтер для команды УП (условный переход).
  /// </summary>
  public class UpCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is UpCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not UpCommandModel up)
        yield break;

      yield return $"{up.CommandNumber} {up.Mnemonic} {up.TargetLabel}";

      // Ключи
      if (up.AlgorithmKey.Count > 0)
        yield return $"\tКлючи команды: {string.Join(", ", up.AlgorithmKey)}";

      // Описание перехода
      if (!string.IsNullOrWhiteSpace(up.TargetLabel))
        yield return $"\tПереход к команде {up.TargetLabel}";
      else
        yield return $"\tПереходная метка не указана!";

      if (up.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
        foreach (var line in up.Comment)
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
