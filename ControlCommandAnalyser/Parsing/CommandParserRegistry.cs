using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Parsing.Interface;

namespace ControlCommandAnalyser.Parsing
{
  /// <summary>
  /// Сервис-реестр для поиска и вызова парсеров команд по мнемонике.
  /// </summary>
  public class CommandParserRegistry
  {
    private readonly Dictionary<string, ICommandParser> _parsers;

    /// <summary>
    /// Инициализация и автоматическая регистрация всех парсеров в сборке.
    /// </summary>
    public CommandParserRegistry()
    {
      _parsers = Assembly.GetExecutingAssembly()
          .GetTypes()
          .Where(t => typeof(ICommandParser).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
          .Select(t => (ICommandParser)Activator.CreateInstance(t)!)
          .ToDictionary(p => p.Mnemonic.ToUpperInvariant());
    }

    /// <summary>
    /// Находит подходящий парсер для заданной мнемоники (без учёта регистра).
    /// </summary>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <returns>Парсер или null.</returns>
    public ICommandParser? FindParser(string mnemonic)
    {
      _parsers.TryGetValue(mnemonic.ToUpperInvariant(), out var parser);
      return parser;
    }
  }
}
