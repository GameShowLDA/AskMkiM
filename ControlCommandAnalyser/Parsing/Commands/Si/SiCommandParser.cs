using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using ControlCommandAnalyser.Domain;
using Utilities.Models;
using static Utilities.LoggerUtility;
using ControlCommandAnalyser.Parsing.Interface;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace ControlCommandAnalyser.Parsing.Commands.Si
{
  /// <summary>
  /// Парсер команды СИ.
  /// Выполняет вызов всех подключенных синтаксических парсеров для анализа строки команды.
  /// Поддерживает фильтрацию парсеров по атрибуту CommandSyntaxAttribute.
  /// </summary>
  public class SiCommandParser : ICommandParser
  {
    /// <summary>
    /// Мнемоника команды СИ.
    /// </summary>
    public string Mnemonic => "СИ";

    private readonly List<ISyntaxParser> _syntaxParsers;

    /// <summary>
    /// Инициализирует парсер СИ.
    /// Выполняет поиск и подключение всех парсеров, помеченных атрибутом CommandSyntax("СИ").
    /// </summary>
    public SiCommandParser()
    {
      _syntaxParsers = Assembly
          .GetExecutingAssembly()
          .GetTypes()
          .Where(t => typeof(ISyntaxParser).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
          .Where(t =>
              t.GetCustomAttributes(typeof(CommandSyntaxAttribute), true)
              .OfType<CommandSyntaxAttribute>()
              .Any(attr => attr.Mnemonic == "СИ"))
          .Select(t => (ISyntaxParser)Activator.CreateInstance(t)!)
          .ToList();
    }

    /// <summary>
    /// Выполняет парсинг блока команды СИ.
    /// Подсвечивает все параметры, а также некорректные (неопознанные) части строки.
    /// </summary>
    /// <param name="block">Блок команды для обработки.</param>
    public async Task ParseAsync(CommandBlock block)
    {
      var line = block.Lines.FirstOrDefault();
      if (string.IsNullOrWhiteSpace(line))
        return;

      block.ExtraHighlights.Clear();

      // Изначально красим всю строку в красный как "по умолчанию"
      block.ExtraHighlights.Add(new HighlightRange(
        line: block.StartLine,
        start: 0,
        length: line.Length,
        target: HighlightTarget.Parameter)
      {
        ColorOverride = ShowMessageModel.ErrorMessage.TitleColor
      });

      // Список найденных параметров
      var foundParameters = new HashSet<string>();

      // Парсим параметры через подключённые парсеры
      foreach (var parser in _syntaxParsers)
      {
        var result = parser.Parse(line, block.StartLine);
        if (result != null)
        {
          block.ExtraHighlights.Add(new HighlightRange(
            line: result.LineIndex,
            start: result.Start,
            length: result.Length,
            target: result.Target)
          {
            ColorOverride = result.Color
          });

          foundParameters.Add(parser.ParameterName);
          LogInformation(result.Description);
        }
      }

      // Всегда подсвечиваем номер команды и мнемонику как корректные
      var match = Regex.Match(line, @"^\s*(\d+)\s+(СИ)", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        // Номер команды
        block.ExtraHighlights.Add(new HighlightRange(
          line: block.StartLine,
          start: match.Groups[1].Index,
          length: match.Groups[1].Length,
          target: HighlightTarget.CommandNumber)
        {
          ColorOverride = Colors.DeepSkyBlue
        });

        // Мнемоника СИ
        block.ExtraHighlights.Add(new HighlightRange(
          line: block.StartLine,
          start: match.Groups[2].Index,
          length: match.Groups[2].Length,
          target: HighlightTarget.Mnemonic)
        {
          ColorOverride = Colors.LightGreen
        });
      }

      // Проверка на корректность команды СИ
      bool voltageFound = foundParameters.Contains("Voltage");
      bool parameterFound = foundParameters.Contains("Mom");
      bool timeFound = foundParameters.Contains("Time");

      if (voltageFound && parameterFound && timeFound)
      {
        block.IsRecognized = true;
        LogInformation($"✅ Команда СИ в строке {block.StartLine + 1} успешно распознана.");
      }
      else
      {
        block.IsRecognized = false;
        LogWarning($"⚠ Команда СИ в строке {block.StartLine + 1} содержит ошибки: напряжение={voltageFound}, X<МОМ={parameterFound}, время={timeFound}");
      }

      await Task.CompletedTask;
    }
  }
}
