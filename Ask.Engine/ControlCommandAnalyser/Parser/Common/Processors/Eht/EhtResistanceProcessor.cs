using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Eht
{
  public class EhtResistanceProcessor : IParameterProcessor<EhtCommandModel>
  {
    public string Process(EhtCommandModel model, string remainder, ParameterContext ctx)
    {
      string lower, higher, unit;
      string cabel, cabelUnit;

      (lower, higher, unit, remainder) = CommonParameterParser.ResistanceParser.ParseResistanceRangeWithR(remainder);

      (cabel, cabelUnit, remainder) = CommonParameterParser.ResistanceParser.ParseCabelResistance(remainder);

      ResistanceManager.ProcessResistance(model, lower, higher, cabel, unit, cabelUnit, ctx.CommandNumber, ctx.Mnemonic, ctx.LineNumber);

      return remainder;
    }
  }
}
