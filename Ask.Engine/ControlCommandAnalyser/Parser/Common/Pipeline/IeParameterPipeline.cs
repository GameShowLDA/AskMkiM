using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ie;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды IE.
  /// Определяет набор процессоров параметров, применяемых к строке команды.
  /// </summary>
  internal class IeParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<IeCommandModel> _pipeline =
        new(new IParameterProcessor<IeCommandModel>[]
        {
            Keys<IeCommandModel>(),
            new IeCapacityProcessor(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды IE.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        IeCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
