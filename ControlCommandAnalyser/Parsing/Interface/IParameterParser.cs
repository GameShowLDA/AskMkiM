using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;
using Utilities.TextEditor;

namespace ControlCommandAnalyser.Parsing.Interface
{
  /// <summary>
  /// Интерфейс для всех парсеров параметров команд.
  /// </summary>
  public interface IParameterParser
  {
    /// <summary>
    /// Пробует найти параметр в строке.
    /// </summary>
    /// <param name="block">Блок команды.</param>
    /// <param name="highlights">Список подсветок, которые добавить.</param>
    /// <returns>true — если найдено, false — если не найдено.</returns>
    bool TryParse(CommandBlock block, out List<HighlightRange> highlights);
  }
}
