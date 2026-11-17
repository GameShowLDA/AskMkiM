using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  internal class PeParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<PeCommandModel> _pipeline =
        new(new IParameterProcessor<PeCommandModel>[]
        {
            Keys<PeCommandModel>(),
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
        PeCommandModel model,
        string remainder,
        ParameterContext ctx,
        IMultimeter meter)
        => _pipeline.Execute(model, remainder, ctx with { Fastmeter = meter });
  }
}
