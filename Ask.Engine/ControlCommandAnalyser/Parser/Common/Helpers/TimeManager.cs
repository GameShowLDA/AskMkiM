using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Менеджер обработки параметра времени.
  /// Выполняет разбор значения времени и при необходимости
  /// подставляет значение по умолчанию.
  /// </summary>
  public static class TimeManager
  {
    /// <summary>
    /// Возвращает числовое значение времени для команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="time">Строковое значение времени.</param>
    /// <param name="unitTime">Единица измерения времени.</param>
    /// <returns>
    /// Значение времени, либо значение по умолчанию,
    /// если указана только единица измерения.
    /// </returns>
    public static double? GetTime(BaseCommandModel model, string time, string unitTime)
    {
      double? timeValue = -1;
      if (!string.IsNullOrEmpty(time) && time != null)
      {
        timeValue = CommonParameterParser.ParseToDouble(time);
      }
      else if (!string.IsNullOrEmpty(unitTime))
      {
        timeValue = 1;
        model.Warnings.Add(GeneralWarnings.DefaultTime(model.StartLineNumber, $"{model.CommandNumber} {model.Mnemonic}", $"{timeValue}{unitTime}"));
      }
      return timeValue;
    }
  }
}
