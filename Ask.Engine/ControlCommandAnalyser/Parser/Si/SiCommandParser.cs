using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;
namespace Ask.Engine.ControlCommandAnalyser.Parser.Si
{
  /// <summary>
  /// Парсер команды СИ (измерение сопротивления изоляции).
  /// <para>
  /// Выполняет полный цикл обработки команды:
  /// <list type="number">
  /// <item><description>Создание модели команды.</description></item>
  /// <item><description>Проверка наличия необходимого оборудования.</description></item>
  /// <item><description>Парсинг параметров через конвейер.</description></item>
  /// <item><description>Построение схемы точек.</description></item>
  /// <item><description>Обработка нераспознанных параметров.</description></item>
  /// </list>
  /// </para>
  /// </summary>
  internal class SiCommandParser : CommandParserBase<SiCommandModel>
  {
    /// <summary>
    /// Определяет, может ли парсер обработать команду
    /// с указанной мнемоникой.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники команды.</param>
    /// <returns>
    /// true — если мнемоника соответствует команде СИ;  
    /// false — если команда должна обрабатываться другим парсером.
    /// </returns>
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.SI);

    /// <summary>
    /// Создаёт и инициализирует модель команды СИ.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Новая модель <see cref="SiCommandModel"/>.</returns>
    protected override SiCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine
    };

    /// <summary>
    /// Выполняет дополнительную проверку после валидации исходных строк.
    /// Проверяет доступность пробойной установки.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Строки команды.</param>
    /// <returns>
    /// true — если можно продолжать парсинг;  
    /// false — если произошла ошибка и дальнейший разбор невозможен.
    /// </returns>
    protected override bool AfterSourceLinesCheck(
      SiCommandModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines)
    {
      var breakdown = BreakdownTesters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
      if (breakdown == null)
      {
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return false;
      }
      return true;
    }

    /// <summary>
    /// Создаёт контекст парсинга параметров.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="model">Модель команды.</param>
    /// <returns>Экземпляр <see cref="ParameterContext"/>.</returns>
    protected override ParameterContext CreateContext(
      string commandNumber,
      string mnemonic,
      int numberLine,
      SiCommandModel model)
      => new(commandNumber, mnemonic, numberLine, BreakdownTesters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault());

    /// <summary>
    /// Выполняет разбор параметров команды СИ через конвейер обработчиков.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Строка с параметрами.</param>
    /// <param name="ctx">Контекст парсинга.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Оставшаяся нераспознанная часть строки.</returns>
    protected override string ParseParameters(SiCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
    {
      var breakdown = ctx.Breakdown;
      if (breakdown == null)
        return remainder;

      return SiParameterPipeline.Execute(model, remainder, ctx, breakdown);
    }

    /// <summary>
    /// Выполняет разбор структуры команды,
    /// формируя схему точек подключения.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="rmCommandModel">Модель команды РМ.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки.</param>
    /// <param name="lines">Исходные строки.</param>
    /// <param name="remainder">Оставшаяся часть строки.</param>
    protected override void ParseStructure(
      SiCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

    /// <summary>
    /// Обрабатывает нераспознанные параметры,
    /// добавляя предупреждения или ошибки в модель.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="numberLine">Номер строки.</param>
    /// <param name="remainder">Нераспознанная часть параметров.</param>
    protected override void HandleUnparsed(SiCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
