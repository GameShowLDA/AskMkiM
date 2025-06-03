using System.Collections.Generic;

namespace ControlCommandAnalyser.Domain
{
  /// <summary>
  /// Представляет блок команды, извлечённый из исходного текста.
  /// Содержит номер команды, мнемонику и все строки, относящиеся к данному блоку.
  /// </summary>
  public class CommandBlock
  {
    /// <summary>
    /// Номер команды (например, "30", "31", "15").
    /// </summary>
    public string CommandNumber { get; set; } = string.Empty;

    /// <summary>
    /// Мнемоника команды (например, "СИ", "ПР", "ЦУ").
    /// </summary>
    public string Mnemonic { get; set; } = string.Empty;

    /// <summary>
    /// Все строки, входящие в данный блок (первая — команда, остальные — параметры/описание).
    /// </summary>
    public List<string> Lines { get; set; } = new();

    /// <summary>
    /// Отформатированные строки блока (если порядок или структура изменены парсером).
    /// Если пусто — использовать Lines.
    /// </summary>
    public List<string> FormattedLines { get; set; } = new();
  }
}
