using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Domain
{
  /// <summary>
  /// Представляет разобранную строку команды из файла ПК.
  /// </summary>
  public class ParsedLine
  {
    /// <summary>
    /// Номер строки в исходном файле.
    /// </summary>
    public int LineIndex { get; set; }

    /// <summary>
    /// Исходный текст строки.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Обнаружен ли номер команды и мнемоника.
    /// </summary>
    public bool HasCommand => CommandNumber != null && Mnemonic != null;

    /// <summary>
    /// Текстовый номер команды (например, "10").
    /// </summary>
    public string? CommandNumber { get; set; }

    /// <summary>
    /// Мнемоника команды (например, "ОК", "ПЭ").
    /// </summary>
    public string? Mnemonic { get; set; }

    /// <summary>
    /// Смещение в строке, где начинается номер команды.
    /// </summary>
    public int? CommandNumberOffset { get; set; }

    /// <summary>
    /// Смещение в строке, где начинается мнемоника команды.
    /// </summary>
    public int? MnemonicOffset { get; set; }
  }
}

