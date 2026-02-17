using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Si
{
  internal class SiVoltageProcessor : IParameterProcessor<SiCommandModel>
  {
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
