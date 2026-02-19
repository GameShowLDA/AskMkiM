using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pr
{
  /// <summary>
  /// Парсер команды ПР (проверка сопротивления).
  /// <para>
  /// Отвечает за полный цикл разбора команды:
  /// <list type="number">
  /// <item><description>Создание модели команды.</description></item>
  /// <item><description>Предобработка исходных строк.</description></item>
  /// <item><description>Парсинг параметров через конвейер.</description></item>
  /// <item><description>Построение схемы подключения.</description></item>
  /// <item><description>Обработка нераспознанных параметров и валидация ключей.</description></item>
  /// </list>
  /// </para>
  /// </summary>
  internal class PrCommandParser : ICommandParser
  {
    /// <summary>
    /// Проверяет, поддерживает ли данный парсер переданную мнемонику команды.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники команды.</param>
    /// <returns>
    /// true — если мнемоника соответствует команде ПР;  
    /// false — если команда должна обрабатываться другим парсером.
    /// </returns>
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.PR);

    /// <summary>
    /// Выполняет полный разбор команды ПР.
    /// <para>
    /// В процессе:
    /// <list type="bullet">
    /// <item><description>Создаётся модель команды.</description></item>
    /// <item><description>Проверяется корректность исходных строк.</description></item>
    /// <item><description>Формируется строка параметров (remainder).</description></item>
    /// <item><description>Запускается конвейер парсинга параметров.</description></item>
    /// <item><description>Строится схема точек.</description></item>
    /// <item><description>Обрабатываются нераспознанные параметры.</description></item>
    /// <item><description>Проверяются допустимые ключи алгоритма.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="commandNumber">Номер команды в программе.</param>
    /// <param name="mnemonic">Мнемоника команды (ПР).</param>
    /// <param name="numberLine">Номер строки, с которой начинается команда.</param>
    /// <param name="lines">Список строк исходного текста команды.</param>
    /// <returns>
    /// Экземпляр <see cref="PrCommandModel"/>  
    /// с заполненными параметрами, схемой и списком ошибок.
    /// </returns>
    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {

      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new PrCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);
      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;
      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);
      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);


      var ctx = ParameterContext.Create(commandNumber, mnemonic, numberLine);
      var meter = new DataBaseConfiguration.Services.Device.FastMeterServices().GetAll().FirstOrDefault();
      remainder = PrParameterPipeline.Execute(model, remainder, ctx, meter);
      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);
      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
