using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors
{
  /// <summary>
  /// Процессор параметра времени.
  /// Извлекает значение времени из строки и записывает его в модель команды.
  /// </summary>
  /// <typeparam name="TModel">Тип модели команды с поддержкой времени.</typeparam>
  internal class TimeProcessor<TModel> : IParameterProcessor<TModel>
      where TModel : BaseCommandModel, ITimeCommandModel
  {
    /// <summary>
    /// Выполняет разбор параметра времени и обновляет модель команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанного параметра времени.</returns>
    public string Process(TModel model, string remainder, ParameterContext ctx)
    {
      var (timeRaw, unitRaw, rest) =
          CommonParameterParser.TimeParser.ParseTime(remainder);

      double? timeValue = TimeManager.GetTime(model, timeRaw, unitRaw);

      if (timeValue.HasValue && timeValue > -1)
        model.Time = timeValue.Value;

      model.TimeSource = timeRaw + unitRaw;

      return rest;
    }
  }
}
