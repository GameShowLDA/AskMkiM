using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров команды.
  /// Последовательно применяет набор процессоров к остаточной строке.
  /// </summary>
  /// <typeparam name="TModel">Тип модели команды.</typeparam>
  public class ParameterPipeline<TModel>
  {
    /// <summary>
    /// Список процессоров параметров, выполняемых по порядку.
    /// </summary>
    private readonly IReadOnlyList<IParameterProcessor<TModel>> _processors;

    /// <summary>
    /// Инициализирует конвейер параметров.
    /// </summary>
    /// <param name="processors">Коллекция процессоров параметров.</param>
    public ParameterPipeline(IEnumerable<IParameterProcessor<TModel>> processors)
    {
      _processors = processors.ToList();
    }

    /// <summary>
    /// Запускает конвейер обработки параметров.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Остаток строки после обработки всеми процессорами.</returns>
    public string Execute(TModel model, string remainder, ParameterContext ctx)
    {
      foreach (var processor in _processors)
      {
        remainder = processor.Process(model, remainder, ctx);
      }

      return remainder;
    }
  }
}
