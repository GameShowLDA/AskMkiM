using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ne;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды НЭ.
  /// Определяет последовательность процессоров параметров команды.
  /// </summary>
  internal class NeParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<NeCommandModel> _pipeline =
        new(new IParameterProcessor<NeCommandModel>[]
        {
            Keys<NeCommandModel>(),
            new NeVoltageProcessor(),
            new AmperageProcessor<NeCommandModel>(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды НЭ.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        NeCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }
}
