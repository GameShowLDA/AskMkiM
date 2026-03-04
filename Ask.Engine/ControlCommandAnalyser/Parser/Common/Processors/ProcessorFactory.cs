using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ParserInterfaces;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors
{
  /// <summary>
  /// Фабрика процессоров параметров.
  /// Предоставляет методы создания стандартных процессоров,
  /// используемых в конвейерах парсинга.
  /// </summary>
  public static class ProcessorFactory
  {
    /// <summary>
    /// Создаёт процессор ключей команды.
    /// </summary>
    /// <typeparam name="TModel">Тип модели команды.</typeparam>
    /// <returns>Экземпляр процессора ключей.</returns>
    public static IParameterProcessor<TModel> Keys<TModel>()
        where TModel : BaseCommandModel
        => new KeyProcessor<TModel>();

    /// <summary>
    /// Создаёт процессор параметра времени.
    /// </summary>
    /// <typeparam name="TModel">Тип модели команды с поддержкой времени.</typeparam>
    /// <returns>Экземпляр процессора времени.</returns>
    public static IParameterProcessor<TModel> Time<TModel>()
        where TModel : BaseCommandModel, ITimeCommandModel
        => new TimeProcessor<TModel>();
  }
}
