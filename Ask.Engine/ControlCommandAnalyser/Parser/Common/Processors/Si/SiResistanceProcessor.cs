using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Si
{
  internal class SiResistanceProcessor : IParameterProcessor<SiCommandModel>
  {
    public string Process(SiCommandModel model, string remainder, ParameterContext ctx)
    {
      var info = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI);

      var (resistance, unit, rest) =
          CommonParameterParser.ResistanceParser.ParseResistance(remainder);

      double value;

      if (string.IsNullOrWhiteSpace(resistance))
      {
        value = 100;
        model.Warnings.Add(
            GeneralWarnings.DefaultResistainceLowLimit(
                model.StartLineNumber,
                $"{ctx.CommandNumber} {ctx.Mnemonic}",
                "100 МОм"));
      }
      else
      {
        value = UnitsConvertor.ConvertToMOhms(
            CommonParameterParser.ParseToDouble(resistance),
            unit);
      }

      if (value > info.UpperLimit || value < info.LowerLimit)
      {
        model.Errors.Add(
            SiErrors.ResistanceLimitsConflict(
                ctx.LineNumber,
                $"{ctx.CommandNumber} {ctx.Mnemonic}",
                "Сопротивление вне диапазона"));
      }

      model.Resistance = value;
      model.ResistanceUnit = "МОм";
      model.ResistanceSource = $"{value}<МОм";

      return rest;
    }
  }
}
