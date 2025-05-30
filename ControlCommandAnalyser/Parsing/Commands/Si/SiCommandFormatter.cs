using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ControlCommandAnalyser.Domain;
using System.Windows.Media;
using ControlCommandAnalyser.Parsing.Interface;

namespace ControlCommandAnalyser.Parsing.Commands.Si
{
  /// <summary>
  /// Форматтер команды СИ.
  /// Формирует строку в правильном порядке: номер команды, СИ, напряжение, сопротивление, время.
  /// </summary>
  [CommandFormatter("СИ")]
  public class SiCommandFormatter : ICommandFormatter
  {
    /// <summary>
    /// Мнемоника команды, для которой применяется форматтер.
    /// </summary>
    public string Mnemonic => "СИ";

    /// <summary>
    /// Формирует строки команды СИ в правильном порядке.
    /// </summary>
    /// <param name="block">Блок команды для форматирования.</param>
    public void Format(CommandBlock block)
    {
      if (!block.IsRecognized) return;

      var parts = new List<string>();

      // Номер команды
      if (!string.IsNullOrWhiteSpace(block.CommandNumber))
        parts.Add(block.CommandNumber);

      // Мнемоника
      if (!string.IsNullOrWhiteSpace(block.Mnemonic))
        parts.Add(block.Mnemonic);

      // Напряжение (Gold)
      var voltage = FindParameterByColor(block, Colors.Gold, @"\b\d{2,4}(В|v|мВ|кВ|MV|KV)\b");
      if (!string.IsNullOrWhiteSpace(voltage))
        parts.Add(voltage);

      // Сопротивление X<МОМ (LightSkyBlue)
      var mom = FindParameterByColor(block, Colors.LightSkyBlue, @"\b\d+<МОМ\b");
      if (!string.IsNullOrWhiteSpace(mom))
        parts.Add(mom);

      // Время Xс (Gold)
      var time = FindParameterByColor(block, Colors.Gold, @"\b\d+[сc]\b");
      if (!string.IsNullOrWhiteSpace(time) && time != voltage) // Чтобы не дублировалось с напряжением
        parts.Add(time);

      // Формируем заголовок команды
      var header = string.Join(" ", parts);

      // Собираем все строки блока
      block.FormattedLines.Clear();
      block.FormattedLines.Add(header);

      // Добавляем все остальные строки блока, кроме первой (если они есть)
      block.FormattedLines.AddRange(block.Lines.Skip(1));
    }


    /// <summary>
    /// Ищет параметр по цвету подсветки и регулярному выражению.
    /// </summary>
    /// <param name="block">Блок команды.</param>
    /// <param name="color">Цвет подсветки.</param>
    /// <param name="pattern">Регулярное выражение для поиска текста.</param>
    /// <returns>Найденное значение или null.</returns>
    private string? FindParameterByColor(CommandBlock block, Color color, string pattern)
    {
      foreach (var line in block.Lines)
      {
        var matches = Regex.Matches(line, pattern, RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
          var highlight = block.ExtraHighlights.FirstOrDefault(h =>
              h.ColorOverride == color &&
              h.Start == match.Index &&
              h.Length == match.Length);

          if (highlight != null)
            return match.Value;
        }
      }
      return null;
    }
  }
}
