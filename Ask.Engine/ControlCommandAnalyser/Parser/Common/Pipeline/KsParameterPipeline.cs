using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ks;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды КС.
  /// Определяет последовательность процессоров параметров и учитывает быстрый измеритель.
  /// </summary>
  internal class KsParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<KsCommandModel> _pipeline =
        new(new IParameterProcessor<KsCommandModel>[]
        {
            Keys<KsCommandModel>(),
            new KsResistanceProcessor(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды КС.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <param name="meter">Экземпляр быстрого измерителя.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        KsCommandModel model,
        string remainder,
        ParameterContext ctx,
        IFastMeter meter)
        => _pipeline.Execute(model, remainder, ctx with { Fastmeter = meter });
  }
}

