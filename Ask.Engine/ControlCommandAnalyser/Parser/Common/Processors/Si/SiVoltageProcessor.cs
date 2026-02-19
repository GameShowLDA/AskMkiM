using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Si
{
  /// <summary>
  /// Процессор параметра напряжения для команды СИ.
  /// Извлекает значение напряжения, проверяет диапазон
  /// и применяет его к модели.
  /// </summary>
  internal class SiVoltageProcessor : IParameterProcessor<SiCommandModel>
  {
    /// <summary>
    /// Выполняет разбор напряжения и обновляет модель команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанного параметра.</returns>
    public string Process(SiCommandModel model, string remainder, ParameterContext ctx)
    {
      var breakdown = ctx.Breakdown!;

      var (voltageRaw, unit, rest) =
          CommonParameterParser.VoltageParser.ParseVoltage(remainder);

      if (string.IsNullOrWhiteSpace(voltageRaw))
      {
        model.Errors.Add(SiErrors.EmptyVoltage(ctx.LineNumber, $"{ctx.CommandNumber} {ctx.Mnemonic}"));
        return rest;
      }

      var value = CommonParameterParser.ParseToDouble(voltageRaw);

      VoltageManager.ApplySingleVoltage(
          model,
          value,
          unit,
          breakdown.IRMinVoltage,
          breakdown.SiMaxVoltage,
          ctx.LineNumber,
          $"{ctx.CommandNumber} {ctx.Mnemonic}");

      return rest;
    }
  }
}
