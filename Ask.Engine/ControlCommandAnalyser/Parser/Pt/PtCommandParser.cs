using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pt
{
  /// <summary>
  /// Парсер команды ПТ.
  /// <para>
  /// Реализует разбор организационной команды ПТ, выполняя:
  /// <list type="number">
  /// <item><description>Создание модели команды.</description></item>
  /// <item><description>Парсинг параметров через конвейер.</description></item>
  /// <item><description>Построение словаря точек по шинам.</description></item>
  /// <item><description>Обработку нераспознанных параметров.</description></item>
  /// </list>
  /// </para>
  /// </summary>
  internal class PtCommandParser : CommandParserBase<PtCommandModel>
  {
    /// <summary>
    /// Проверяет, может ли парсер обработать переданную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники команды.</param>
    /// <returns>
    /// true — если мнемоника соответствует команде ПТ;  
    /// false — если команда не поддерживается данным парсером.
    /// </returns>
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.PT);

    /// <summary>
    /// Создаёт и инициализирует модель команды ПТ.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>
    /// Новый экземпляр <see cref="PtCommandModel"/> с заполненными базовыми свойствами.
    /// </returns>
    protected override PtCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine
    };

    /// <summary>
    /// Выполняет парсинг параметров команды через конвейер обработчиков.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Строка с параметрами для разбора.</param>
    /// <param name="ctx">Контекст парсинга.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>
    /// Остаток строки после извлечения распознанных параметров.
    /// </returns>
    protected override string ParseParameters(PtCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
      => PtParameterPipeline.Execute(model, remainder, ctx);

    /// <summary>
    /// Выполняет разбор структурной части команды — точек подключения.
    /// <para>
    /// В результате формируется словарь:
    /// шина → список точек.
    /// </para>
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="rmCommandModel">Модель команды РМ с конфигурацией точек.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <param name="remainder">
    /// Остаток строки; после выполнения из него удаляется блок точек.
    /// </param>
    protected override void ParseStructure(
      PtCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.BusPointsDictionary =
        SchemeManager.GetBusPointsDictionary(model, rmCommandModel, numberLine, commandNumber, mnemonic, ref remainder);

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды.
    /// <para>
    /// Добавляет предупреждения и ошибки в модель,
    /// если после парсинга осталась непроанализированная часть строки.
    /// </para>
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="remainder">Нераспознанная часть строки.</param>
    protected override void HandleUnparsed(PtCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
