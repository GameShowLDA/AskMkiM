using ControlCommandAnalyser.Parsing;

namespace ControlCommandAnalyser.Domain
{
  public class CommandBlock
  {
    public int StartLine { get; set; }
    public List<string> Lines { get; set; } = new();

    /// <summary>
    /// Распознанная мнемоника команды, если найдена.
    /// </summary>
    public string? Mnemonic { get; set; }

    /// <summary>
    /// Номер команды, извлечённый из начала строки.
    /// </summary>
    public string? CommandNumber { get; set; }

    /// <summary>
    /// Признак, была ли команда успешно распознана.
    /// </summary>
    public bool IsRecognized { get; set; } = false;

    /// <summary>
    /// Список диапазонов дополнительной подсветки.
    /// </summary>
    public List<HighlightRange> ExtraHighlights { get; set; } = new();
  }
}
