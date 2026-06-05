using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Pr;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды ПР.
  /// Определяет набор процессоров параметров и использует быстрый измеритель.
  /// </summary>
  internal class PrParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<PrCommandModel> _pipeline =
        new(new IParameterProcessor<PrCommandModel>[]
        {
            Keys<PrCommandModel>(),
            new PrResistanceProcessor(),
            new AmperageProcessor<PrCommandModel>(),
            new PrVoltageProcessor(),
            Time<PrCommandModel>(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды ПР.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <param name="meter">Экземпляр быстрого измерителя.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        PrCommandModel model,
        string remainder,
        ParameterContext ctx,
        IFastMeter meter)
        => _pipeline.Execute(model, remainder, ctx with { Fastmeter = meter });
  }
}
