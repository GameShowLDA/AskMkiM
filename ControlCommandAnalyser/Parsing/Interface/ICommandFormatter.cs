using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;

namespace ControlCommandAnalyser.Parsing.Interface
{
  /// <summary>
  /// Интерфейс для форматирования строки команды после парсинга.
  /// </summary>
  public interface ICommandFormatter
  {
    /// <summary>
    /// Мнемоника команды, для которой применяется форматтер.
    /// </summary>
    string Mnemonic { get; }

    /// <summary>
    /// Формирует итоговую строку команды в правильном порядке.
    /// </summary>
    /// <param name="block">Блок команды, который нужно отформатировать.</param>
    void Format(CommandBlock block);
  }
}
