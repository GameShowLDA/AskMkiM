using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Si
{
  internal class SiVoltageProcessor : IParameterProcessor<SiCommandModel>
  {
    public string Process(SiCommandModel model, string remainder, ParameterContext ctx)
    {
      var breakdown = ctx.Breakdown!;
      var (voltage, unit, rest) =
          CommonParameterParser.VoltageParser.ParseVoltage(remainder);

      if (string.IsNullOrWhiteSpace(voltage))
      {
        model.Errors.Add(SiErrors.EmptyVoltage(ctx.LineNumber, $"{ctx.CommandNumber} {ctx.Mnemonic}"));
        return rest;
      }

      var value = CommonParameterParser.ParseToDouble(voltage);
      model.Voltage = value;

      if (value > breakdown.SiMaxVoltage)
      {
        model.Errors.Add(
            GeneralErrors.VoltageConflict(
                ctx.LineNumber,
                $"{ctx.CommandNumber} {ctx.Mnemonic}",
                "Напряжение превышает максимально допустимое"));
      }
      else if (value < breakdown.IRMinVoltage)
      {
        model.Errors.Add(
            GeneralErrors.VoltageConflict(
                ctx.LineNumber,
                $"{ctx.CommandNumber} {ctx.Mnemonic}",
                "Напряжение меньше минимально допустимого"));
      }

      model.VoltageSource = $"{value}{unit}";
      return rest;
    }
  }
}
