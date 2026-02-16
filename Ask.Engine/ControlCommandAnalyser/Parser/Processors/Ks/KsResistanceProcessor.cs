using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Processors.Ks
{
  internal class KsResistanceProcessor : IParameterProcessor<KsCommandModel>
  {
    public string Process(KsCommandModel model, string remainder, ParameterContext ctx)
    {
      string lowerRaw, higherRaw, unit;

      (lowerRaw, higherRaw, unit, remainder) =
          CommonParameterParser.ResistanceParser.ParseResistanceRange(remainder);

      LogDebug($"После парсинга сопротивления: нижняя='{lowerRaw}', верхняя='{higherRaw}', единица='{unit}', remainder='{remainder}'");

      if (string.IsNullOrEmpty(lowerRaw) && string.IsNullOrEmpty(higherRaw))
      {
        model.Errors.Add(KsErrors.EmptyResistance(ctx.LineNumber, $"{ctx.CommandNumber} {ctx.Mnemonic}"));
        LogWarning($"Не указано сопротивление (строка {ctx.LineNumber}): {ctx.CommandNumber} {ctx.Mnemonic}");
        return remainder;
      }

      var meter = ctx.Fastmeter;
      if (meter == null)
      {
        LogError("Не найден быстрый измеритель.");
        model.Errors.Add(GeneralErrors.FastMeterNotFound(ctx.LineNumber, $"{ctx.CommandNumber} {ctx.Mnemonic}"));
        return remainder;
      }

      ResistanceManager.ProcessResistance(model, lowerRaw, higherRaw, unit, ctx.CommandNumber, ctx.Mnemonic, ctx.LineNumber);

      return remainder;
    }
  }
}
