using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Eht;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды ЭТ.
  /// Определяет последовательность процессоров параметров.
  /// </summary>
  public static class EhtParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<EhtCommandModel> _pipeline =
        new(new IParameterProcessor<EhtCommandModel>[]
        {
            Keys<EhtCommandModel>(),
            new EhtResistanceProcessor(),
            new AmperageProcessor<EhtCommandModel>(),
            Time<EhtCommandModel>(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды ЭХТ.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        EhtCommandModel model,
        string remainder,
        ParameterContext ctx)
        => _pipeline.Execute(model, remainder, ctx);
  }

}
