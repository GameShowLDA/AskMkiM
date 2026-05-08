using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды ПТ.
  /// Определяет последовательность процессоров параметров команды.
  /// </summary>
  internal class PtParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<PtCommandModel> _pipeline =
        new(new IParameterProcessor<PtCommandModel>[]
        {
          Keys<PtCommandModel>(),
          Time<PtCommandModel>(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды ПТ.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        PtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
