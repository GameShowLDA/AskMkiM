using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Si;
using static Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.ProcessorFactory;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline
{
  /// <summary>
  /// Конвейер обработки параметров для команды СИ.
  /// Определяет последовательность процессоров параметров и учитывает пробойную установку.
  /// </summary>
  internal static class SiParameterPipeline
  {
    /// <summary>
    /// Внутренний конвейер процессоров параметров.
    /// </summary>
    private static readonly ParameterPipeline<SiCommandModel> _pipeline =
        new(new IParameterProcessor<SiCommandModel>[]
        {
            Keys<SiCommandModel>(),
            new SiVoltageProcessor(),
            new SiResistanceProcessor(),
            Time<SiCommandModel>(),
        });

    /// <summary>
    /// Выполняет обработку параметров команды СИ.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <param name="breakdown">Экземпляр пробойной установки.</param>
    /// <returns>Остаток строки после выполнения конвейера.</returns>
    public static string Execute(
        SiCommandModel model,
        string remainder,
        ParameterContext ctx,
        IBreakdownTester breakdown)
        => _pipeline.Execute(model, remainder, ctx with { Breakdown = breakdown });
  }

}
