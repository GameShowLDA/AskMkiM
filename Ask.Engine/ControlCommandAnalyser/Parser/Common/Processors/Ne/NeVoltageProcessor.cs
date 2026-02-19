using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ne
{
  internal class NeVoltageProcessor : IParameterProcessor<NeCommandModel>
  {
    public string Process(NeCommandModel model, string remainder, ParameterContext ctx)
    {
      string lowerRaw, higherRaw, unitRange;

      (lowerRaw, higherRaw, unitRange, remainder) =
          CommonParameterParser.VoltageParser.ParseVoltageRange(remainder);

      double? lower = !string.IsNullOrWhiteSpace(lowerRaw)
          ? CommonParameterParser.ParseToDouble(lowerRaw)
          : null;

      double? higher = !string.IsNullOrWhiteSpace(higherRaw)
          ? CommonParameterParser.ParseToDouble(higherRaw)
          : null;

      var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.NE);

      if (lower.HasValue || higher.HasValue)
      {
        VoltageManager.ApplyRange(
            model,
            unitRange,
            lower,
            higher,
            commandInfo.LowerLimit,
            commandInfo.UpperLimit);
      }

      LogDebug($"После парсинга диапазона напряжения: {lowerRaw} - {higherRaw} {unitRange}");

      string voltageRaw, unitSingle;

      (voltageRaw, unitSingle, remainder) =
          CommonParameterParser.VoltageParser.ParseVoltage(remainder);

      if (!string.IsNullOrWhiteSpace(voltageRaw))
      {
        double value = CommonParameterParser.ParseToDouble(voltageRaw);

        VoltageManager.ApplyOperatingVoltage(
            model,
            value,
            unitSingle);

        LogDebug($"Распознано рабочее напряжение: {value}{unitSingle}");
      }
      else
      {
        if (!model.AlgorithmKey.Contains(AlgorithmKey.Н.ToString()))
        {
          VoltageManager.ApplyDefaultVoltage(
              model,
              commandInfo.UpperLimit,
              commandInfo.Unit);

          LogDebug($"Установлено дефолтное напряжение: {commandInfo.UpperLimit}{commandInfo.Unit}");
        }
      }

      return remainder;
    }
  }
}
