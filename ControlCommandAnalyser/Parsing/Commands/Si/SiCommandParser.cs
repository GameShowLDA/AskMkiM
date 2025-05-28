using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using ControlCommandAnalyser.Domain;
using Utilities.Models;
using static Utilities.LoggerUtility;

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
    /// Использует все подключенные парсеры для поиска параметров в строке.
    /// Добавляет подсветку для найденных элементов.
    /// В случае отсутствия обязательных параметров подсвечивает мнемонику СИ красным цветом.
    /// </summary>
    /// <param name="block">Блок команд для обработки.</param>
    public async Task ParseAsync(CommandBlock block)
    {
      var line = block.Lines.FirstOrDefault();
      if (string.IsNullOrWhiteSpace(line))
        return;

      block.ExtraHighlights.Clear();

      bool voltageFound = false;
      bool parameterFound = false;
      bool timeFound = false;

      foreach (var parser in _syntaxParsers)
      {
        var result = parser.Parse(line, block.StartLine);
        if (result != null)
        {
          block.ExtraHighlights.Add(new HighlightRange(
              line: result.LineIndex,
              start: result.Start,
              length: result.Length,
              target: result.Target
          )
          {
            ColorOverride = result.Color
          });

          LogInformation(result.Description);

          // Флаги найденных параметров
          if (parser is VoltageParser) voltageFound = true;
          if (parser is ParameterMomParser) parameterFound = true;
          if (parser is TimeParser) timeFound = true;
        }
      }

      // Проверка на корректность команды СИ
      if (voltageFound && parameterFound && timeFound)
      {
        block.IsRecognized = true;
      }
      else
      {
        block.IsRecognized = false;
        HighlightSiAsError(block, line);
        LogWarning($"⚠ Команда СИ в строке {block.StartLine + 1} не содержит напряжения, параметра X<МОМ или времени.");
      }

      await Task.CompletedTask;
    }

    /// <summary>
    /// Добавляет подсветку на мнемонику СИ красным цветом, если команда некорректна.
    /// </summary>
    /// <param name="block">Блок команды, в который добавляется подсветка.</param>
    /// <param name="line">Исходная строка команды.</param>
    private void HighlightSiAsError(CommandBlock block, string line)
    {
      int siIndex = line.IndexOf("СИ", StringComparison.OrdinalIgnoreCase);
      if (siIndex >= 0)
      {
        block.ExtraHighlights.Add(new HighlightRange(
            line: block.StartLine,
            start: siIndex,
            length: 2,
            target: HighlightTarget.Mnemonic
        )
        {
          ColorOverride = ShowMessageModel.ErrorMessage.TitleColor
        });
      }
    }
  }
}
