using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Rm
{
  /// <summary>
  /// Парсер команды РМ.
  /// <para>
  /// Отвечает за разбор команды конфигурации точек,
  /// формируя отображение точек объекта контроля на входы системы.
  /// </para>
  /// <para>
  /// Основные задачи:
  /// <list type="number">
  /// <item><description>Проверка соответствия мнемоники.</description></item>
  /// <item><description>Удаление комментариев и нормализация строк.</description></item>
  /// <item><description>Парсинг выражений точек.</description></item>
  /// <item><description>Заполнение словаря соответствия точек.</description></item>
  /// </list>
  /// </para>
  /// </summary>
  public class RmCommandParser : ICommandParser
  {
    /// <summary>
    /// Определяет, может ли текущий парсер обработать команду
    /// с указанной мнемоникой.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники команды.</param>
    /// <returns>
    /// true — если мнемоника соответствует команде РМ;  
    /// false — если команда не поддерживается данным парсером.
    /// </returns>
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.RM);

    /// <summary>
    /// Выполняет разбор команды РМ и формирует модель.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>
    /// Экземпляр <see cref="RmCommandModel"/>,
    /// содержащий карту соответствия точек и исходные данные.
    /// </returns>
    /// <remarks>
    /// В процессе выполнения:
    /// <list type="bullet">
    /// <item><description>Удаляются комментарии.</description></item>
    /// <item><description>Очищается префикс команды из первой строки.</description></item>
    /// <item><description>Объединяются строки тела команды.</description></item>
    /// <item><description>Выполняется парсинг всех выражений точек.</description></item>
    /// <item><description>Заполняется словарь <c>PointsMap</c>.</description></item>
    /// </list>
    /// </remarks>
    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new RmCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var sb = new System.Text.StringBuilder();
      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      lines.Clear();
      lines.AddRange(processedLines);
      for (int i = 0; i < lines.Count; i++)
      {
        var line = lines[i].Trim();
        if (i == 0)
        {
          var match = System.Text.RegularExpressions.Regex.Match(line, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
          if (match.Success) line = match.Groups[1].Value.Trim();
        }
        if (!string.IsNullOrWhiteSpace(line))
        {
          sb.AppendLine(line);
          model.PointsSourse = line;
        }
      }

      var pairs = RmExpressionParser.ParseAllExpressions(sb.ToString(), ref model);

      foreach (var pair in pairs)
        model.PointsMap[pair.OkPoint] = pair.AskInput;

      return model;
    }
  }
}
