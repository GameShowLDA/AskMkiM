using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors
{
  /// <summary>
  /// Универсальный процессор ключей команды.
  /// Извлекает ключи алгоритма из строки и применяет их к модели.
  /// </summary>
  /// <typeparam name="TModel">Тип модели команды.</typeparam>
  internal class KeyProcessor<TModel> : IParameterProcessor<TModel>
      where TModel : BaseCommandModel
  {
    /// <summary>
    /// Выполняет разбор ключей из строки команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанных ключей.</returns>
    public string Process(TModel model, string remainder, ParameterContext ctx)
        => KeyParser.ParseKeys(ctx.LineNumber, model, remainder);
  }
}
