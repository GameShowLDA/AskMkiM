using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common
{
  /// <summary>
  /// Базовый абстрактный класс парсера управляющих команд.
  /// </summary>
  /// <typeparam name="TModel">
  /// Тип модели команды. Должен наследоваться от <see cref="BaseCommandModel"/>.
  /// </typeparam>
  /// <remarks>
  /// Реализует шаблон проектирования <b>Template Method</b>.
  /// Определяет общий конвейер разбора команды и предоставляет точки расширения
  /// для конкретных реализаций парсеров.
  /// </remarks>
  internal abstract class CommandParserBase<TModel> : ICommandParser where TModel : BaseCommandModel
  {
    /// <summary>
    /// Проверяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    public abstract bool CanParse(MnemonicIdentifier mnemonic);

    /// <summary>
    /// Выполняет полный цикл парсинга команды и возвращает модель.
    /// </summary>
    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = CreateModel(commandNumber, numberLine, lines);

      if (!BeforeCheckRm(model, commandNumber, mnemonic, numberLine, lines))
        return model;

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);

      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;

      if (!AfterSourceLinesCheck(model, commandNumber, mnemonic, numberLine, lines))
        return model;

      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);

      if (ShouldRemoveCommandPrefix(model))
        remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

      var ctx = CreateContext(commandNumber, mnemonic, numberLine, model);
      remainder = ParseParameters(model, remainder, ctx, lines);

      if (!AfterParametersParsed(model, commandNumber, mnemonic, numberLine, lines, ref remainder))
        return model;

      ParseStructure(model, rmCommandModel, commandNumber, mnemonic, numberLine, lines, ref remainder);

      HandleUnparsed(model, numberLine, remainder);

      if (ShouldValidateAllowedKeys(model))
        AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");
      AfterParse(model, commandNumber, mnemonic, numberLine, lines);

      return model;
    }

    /// <summary>Создаёт модель команды.</summary>
    protected abstract TModel CreateModel(string commandNumber, int numberLine, List<string> lines);

    /// <summary>
    /// Вызывается перед проверкой RM-точек.
    /// </summary>
    /// <returns>
    /// <c>true</c> — продолжить парсинг; <c>false</c> — завершить.
    protected virtual bool BeforeCheckRm(
      TModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines) => true;

    /// <summary>
    /// Вызывается после проверки исходных строк.
    /// </summary>
    /// <returns>
    /// <c>true</c> — продолжить парсинг; <c>false</c> — завершить.
    /// </returns>
    protected virtual bool AfterSourceLinesCheck(
      TModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines) => true;

    /// <summary>
    /// Определяет, нужно ли удалять префикс команды перед парсингом.
    /// </summary>
    protected virtual bool ShouldRemoveCommandPrefix(TModel model) => true;

    /// <summary>
    /// Создаёт контекст парсинга параметров.
    /// </summary>
    protected virtual ParameterContext CreateContext(
      string commandNumber,
      string mnemonic,
      int numberLine,
      TModel model) => ParameterContext.Create(commandNumber, mnemonic, numberLine);

    /// <summary>
    /// Разбирает параметры команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся строка команды.</param>
    /// <param name="ctx">Контекст параметров.</param>
    /// <param name="lines">Исходные строки.</param>
    /// <returns>Неразобранный остаток строки.</returns>
    protected abstract string ParseParameters(
      TModel model,
      string remainder,
      ParameterContext ctx,
      List<string> lines);

    /// <summary>
    /// Вызывается после парсинга параметров.
    /// </summary>
    /// <returns>
    /// <c>true</c> — продолжить парсинг; <c>false</c> — завершить.
    /// </returns>
    protected virtual bool AfterParametersParsed(
      TModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder) => true;

    /// <summary>
    /// Разбирает структурную часть команды.
    /// </summary>
    protected abstract void ParseStructure(
      TModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder);

    /// <summary>
    /// Обрабатывает нераспознанный остаток команды.
    /// </summary>
    protected abstract void HandleUnparsed(TModel model, int numberLine, string remainder);

    /// <summary>
    /// Определяет, нужно ли выполнять валидацию допустимых ключей.
    /// </summary>
    protected virtual bool ShouldValidateAllowedKeys(TModel model) => true;

    /// <summary>
    /// Вызывается после завершения парсинга.
    /// </summary>
    protected virtual void AfterParse(
      TModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines)
    {
    }
  }
}
