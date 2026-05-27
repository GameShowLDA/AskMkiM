using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Pr
{
  internal class PrVoltageProcessor : IParameterProcessor<PrCommandModel>
  {
    /// <summary>
    /// Выполняет разбор напряжения и обновляет модель команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанного параметра.</returns>
    public string Process(PrCommandModel model, string remainder, ParameterContext ctx)
    {
      var breakdown = ctx.Breakdown!;

      var (voltageRaw, unit, rest) =
          CommonParameterParser.VoltageParser.ParseVoltage(remainder);

      if (string.IsNullOrWhiteSpace(voltageRaw))
      {
        if (model.HasAmperage)
        {
          var description = "Напряжение не указано";
          model.Errors.Add(GeneralErrors.VoltageConflict(ctx.LineNumber, $"{model.CommandNumber} {model.Mnemonic}", description));
        }
        return rest;
      }
      else
      {
        var value = CommonParameterParser.ParseToDouble(voltageRaw);
      }
      if (model is BaseCommandModel baseCommand)
      {
        baseCommand.Warnings.Add(GeneralWarnings.IgnoreVoltage(baseCommand.StartLineNumber, $"{baseCommand.CommandNumber} {baseCommand.Mnemonic}"));
      }
      LogDebug($"Распознано напряжение: {voltageRaw}{unit}");

      return rest;
    }
  }
}
