using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Pi;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды ПИ.
  /// Определяет последовательность процессоров и учитывает пробойную установку.
  /// </summary>
  internal class PiParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<PiCommandModel> _pipeline =
        new(new IParameterProcessor<PiCommandModel>[]
        {
            Keys<PiCommandModel>(),
            new PiVoltageProcessor(),
            Time<PiCommandModel>(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды ПИ.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <param name="breakdown">Экземпляр установки пробоя.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        PiCommandModel model,
        string remainder,
        ParameterContext ctx,
        IBreakdownTester breakdown)
        => _pipeline.Execute(model, remainder, ctx with { Breakdown = breakdown });
  }
}
