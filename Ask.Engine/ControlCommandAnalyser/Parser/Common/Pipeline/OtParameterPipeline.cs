using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды ОТ.
  /// Определяет набор процессоров параметров команды.
  /// </summary>
  internal class OtParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<OtCommandModel> _pipeline =
        new(new IParameterProcessor<OtCommandModel>[]
        {
            Keys<OtCommandModel>(),
            Time<OtCommandModel>(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды ОТ.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        OtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
