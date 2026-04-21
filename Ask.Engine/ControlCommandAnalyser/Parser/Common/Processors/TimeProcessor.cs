using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.DataBase.Provider.Migrations;
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
      int starIndex = remainder.IndexOf('*');

      string header = starIndex >= 0 ? remainder.Substring(0, starIndex) : remainder;
      string tail = starIndex >= 0 ? remainder.Substring(starIndex) : "";

      var (timeRaw, unitRaw, rest) =
          CommonParameterParser.TimeParser.ParseTime(header);

      double? timeValue = TimeManager.GetTime(model, timeRaw, unitRaw);

      if (timeValue.HasValue && timeValue > -1)
        model.Time = timeValue.Value;

      model.TimeSource = timeRaw + unitRaw;
      if ((model is SiCommandModel || model is PiCommandModel)
          && timeValue.GetValueOrDefault(-1) < 0)
      {
        model.Time = 5;
        model.TimeSource = "5C";
      }

      return $"{rest}{tail}";
    }
  }
}
