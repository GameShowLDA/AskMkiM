using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using DataBaseConfiguration.Migrations;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Pi
{
  internal class PiVoltageProcessor : IParameterProcessor<PiCommandModel>
  {
    public string Process(PiCommandModel model, string remainder, ParameterContext ctx)
    {
      var breakdown = ctx.Breakdown!;
      var (voltage, unit, rest) =
          CommonParameterParser.VoltageParser.ParseVoltage(remainder);

      var maxDCWVoltage = MeasurementTypeCommand.PI_DCW.GetDisplayInfo().UpperLimit;
      var minVoltage = MeasurementTypeCommand.PI_ACW.GetDisplayInfo().LowerLimit;
      var maxACWVoltage = MeasurementTypeCommand.PI_ACW.GetDisplayInfo().UpperLimit;

      if (string.IsNullOrWhiteSpace(voltage))
      {
        model.Errors.Add(SiErrors.EmptyVoltage(ctx.LineNumber, $"{ctx.CommandNumber} {ctx.Mnemonic}"));
        return rest;
      }

      var value = CommonParameterParser.ParseToDouble(voltage);
      model.Voltage = value;

      bool isDcw = rest.Contains('+');
      if (isDcw)
      {
        model.VoltageType = VoltageEnum.Type.DCW;
        rest = rest.Replace("+", string.Empty);
      }
      else
      {
        model.VoltageType = VoltageEnum.Type.ACW;
      }

      model.VoltageSource = voltage;

      if (model.VoltageSource != null)
      {
        model.Voltage = CommonParameterParser.ParseToDouble(model.VoltageSource);
        model.VoltageSource += unit;
        var maxVoltage = maxACWVoltage;
        var voltageType = string.Empty;
        if (model.VoltageType == VoltageEnum.Type.DCW)
        {
          maxVoltage = maxDCWVoltage;
        }
        voltageType = model.VoltageType == VoltageEnum.Type.DCW ? "постоянного" : "переменного";
        var voltageValue = UnitsConvertor.TryConvertBack(model.Voltage.Value, unit);

        if (value > maxVoltage)
        {
          var maxValue = UnitsConvertor.TryConvertBack(breakdown.PiMaxVoltage, "В");
          LogError($"В команде ПИ указано напряжение, превышающее максимально допустимое напряжение пробойной установки.");
          var description = $"В команде {model.CommandNumber} {model.Mnemonic} указано напряжение ({voltageValue.Item1} {voltageValue.Item2}), " +
            $"превышающий максимально допустимое напряжение пробойной установки ({maxValue.Item1} {maxValue.Item2}  " +
            $"для {voltageType} тока).";
          model.Errors.Add(GeneralErrors.VoltageConflict(ctx.LineNumber, $"{model.CommandNumber} {model.Mnemonic}", description));
        }
        else if (value < minVoltage)
        {
          var minValue = UnitsConvertor.TryConvertBack(minVoltage, "В");
          LogError($"В команде ПИ указано напряжение, меньше минимально допустимого напряжения пробойной установки.");
          var description = $"В команде {model.CommandNumber} {model.Mnemonic} указано напряжение ({voltageValue.Item1} {voltageValue.Item2}), " +
            $" меньше минимально допустимого напряжения пробойной установки ({minValue.Item1} {minValue.Item2}" +
            $" для {voltageType} тока).";
          model.Errors.Add(GeneralErrors.VoltageConflict(ctx.LineNumber, $"{model.CommandNumber} {model.Mnemonic}", description));
        }
      }
      else
      {
        model.Voltage = model.Voltage.Value;
        model.VoltageSource = model.Voltage.Value.ToString() + unit;
      }
      return rest;
    }
  }
}
