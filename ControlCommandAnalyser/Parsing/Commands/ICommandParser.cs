using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;

namespace ControlCommandAnalyser.Parsing.Commands
{
  /// <summary>
  /// Интерфейс транслятора одной команды.
  /// </summary>
  public interface ICommandParser
  {
    /// <summary>
    /// Мнемоника, с которой работает парсер (например, "ОК", "ПЭ").
    /// </summary>
    string Mnemonic { get; }

    /// <summary>
    /// Обрабатывает команду и выводит результат.
    /// </summary>
    Task ParseAsync(CommandBlock block);
  }
}

