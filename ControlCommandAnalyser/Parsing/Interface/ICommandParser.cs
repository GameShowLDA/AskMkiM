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
  /// Интерфейс для всех парсеров команд. Каждый парсер отвечает за обработку конкретной мнемоники.
  /// </summary>
  public interface ICommandParser
  {
    /// <summary>
    /// Мнемоника (короткое имя) команды, для которой предназначен этот парсер.
    /// </summary>
    string Mnemonic { get; }

    /// <summary>
    /// Парсит командный блок, возвращает новый (или модифицированный) блок и подсветку.
    /// </summary>
    /// <param name="originalBlock">Исходный блок.</param>
    /// <param name="highlights">Список диапазонов подсветки.</param>
    /// <returns>Модифицированный блок (или новый), либо null, если не удалось распознать.</returns>
    CommandBlock? Parse(CommandBlock originalBlock, out List<HighlightRange> highlights);
  }
}
