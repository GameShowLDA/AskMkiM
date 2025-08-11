using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Formatter
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

      yield return string.Empty;
    }
  }
}
