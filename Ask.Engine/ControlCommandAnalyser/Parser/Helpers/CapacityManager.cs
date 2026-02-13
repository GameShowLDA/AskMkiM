using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using Ask.Core.Shared.Metadata.Atributes;
using static Ask.LogLib.LoggerUtility;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Windows.Navigation;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  public static class CapacityManager
  {
    public static IeCommandModel ProcessCapacity(IeCommandModel model, string lowerLimitCapacity,
  string higherLimitCapacity, string unit, int numberLine, string commandNumber, string mnemonic)
    {
      double? lower = CommonParameterParser.ParseToDouble(lowerLimitCapacity);
      double? higher = !string.IsNullOrWhiteSpace(higherLimitCapacity)
          ? CommonParameterParser.ParseToDouble(higherLimitCapacity)
          : null;

      var meter = GetFastMeter(model, numberLine, commandNumber, mnemonic);
      if (meter == null)
        return model;

      var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.IE);

      var limits = GetCapacityLimits(commandInfo);

      bool hasErrors = false;

      hasErrors |= ValidateLowerHigherRelation(
          lower, higher, unit, numberLine, commandNumber, mnemonic, model);

      hasErrors |= ValidateLowerLimit(
          lower, unit, limits, numberLine, commandNumber, mnemonic, model);

      hasErrors |= ValidateHigherLimit(
          higher, unit, limits, numberLine, commandNumber, mnemonic, model);

      if (!hasErrors)
      {
        return ApplyCapacityToModel(model, lower.Value, higher, unit, limits.MaxCapacity, numberLine, commandNumber, mnemonic);
      }

      return model;
    }

    private static IFastMeter GetFastMeter(IeCommandModel model, int numberLine, string commandNumber, string mnemonic)
    {
      var meter = new DataBaseConfiguration.Services.Device.FastMeterServices()
          .GetAll()
          .FirstOrDefault();

      if (meter == null)
      {
        LogError("Не найден быстрый измеритель.");
        model.Errors.Add(
            GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
      }

      return meter;
    }

    private static (double MinCapacity, double MaxCapacity, double LowerLimit, double HigherLimit, string DefaultUnit) GetCapacityLimits(CommandDisplayInfoAttribute commandInfo)
    {
      double minCapacity = commandInfo.LowerLimit;
      double maxCapacity = commandInfo.UpperLimit;

      var lowerLimit = UnitsConvertor.TryParseValue($"{minCapacity}", commandInfo.Unit);
      var higherLimit = UnitsConvertor.TryParseValue($"{maxCapacity}", commandInfo.Unit);

      return (minCapacity, maxCapacity, lowerLimit.Value, higherLimit.Value, commandInfo.Unit);
    }

    private static bool ValidateLowerHigherRelation(double? lower, double? higher, string unit, int numberLine, string commandNumber, string mnemonic, BaseCommandModel model)
    {
      if (lower.HasValue && higher.HasValue && lower.Value >= higher.Value)
      {
        var lowerValue = UnitsConvertor.TryConvertBack(lower.Value, unit);
        var higherValue = UnitsConvertor.TryConvertBack(higher.Value, unit);

        LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) " +
                   $"нижняя граница больше или равна верхней.");

        model.Errors.Add(
            IeErrors.CapacityLimitsConflict(
                numberLine,
                $"{commandNumber} {mnemonic}",
                $"Нижняя граница ({lowerValue.Item1} {lowerValue.Item2}) " +
                $"больше или равна верхней ({higherValue.Item1} {higherValue.Item2})."));

        return true;
      }

      return false;
    }
    private static bool ValidateLowerLimit(double? lower, string unit,
    (double MinCapacity, double MaxCapacity, double LowerLimit, double HigherLimit, string DefaultUnit) limits,
    int numberLine, string commandNumber, string mnemonic, IeCommandModel model)
    {
      if (!lower.HasValue)
        return false;

      var lowerValue = UnitsConvertor.TryParseValue($"{lower.Value}", unit);

      if (lowerValue < limits.LowerLimit)
      {
        AddLowerTooSmallError(model, numberLine, commandNumber, mnemonic,
            lowerValue.Value, unit, limits);
        return true;
      }

      if (lowerValue > limits.HigherLimit)
      {
        AddLowerTooLargeError(model, numberLine, commandNumber, mnemonic,
            lowerValue.Value, unit, limits);
        return true;
      }

      return false;
    }

    private static bool ValidateHigherLimit(double? higher, string unit,
    (double MinCapacity, double MaxCapacity, double LowerLimit, double HigherLimit, string DefaultUnit) limits,
    int numberLine, string commandNumber, string mnemonic, IeCommandModel model)
    {
      if (!higher.HasValue)
        return false;

      var higherValue = UnitsConvertor.TryParseValue($"{higher.Value}", unit);

      if (higherValue > limits.HigherLimit)
      {
        AddHigherTooLargeError(model, numberLine, commandNumber, mnemonic,
            higherValue.Value, unit, limits);
        return true;
      }

      if (higherValue < limits.LowerLimit)
      {
        AddHigherTooSmallError(model, numberLine, commandNumber, mnemonic,
            higherValue.Value, unit, limits);
        return true;
      }

      return false;
    }
    private static IeCommandModel ApplyCapacityToModel(IeCommandModel model, double lower, double? higher, string unit, double maxCapacity, int numberLine, string commandNumber, string mnemonic)
    {
      var lowerValue = UnitsConvertor.TryConvertBack(lower, unit);
      model.CapacityUnit = lowerValue.Item2 ?? string.Empty;
      model.LowerLimitCapacity = lowerValue.Item1;
      model.LowerLimitCapacitySource = $"{lowerValue.Item1} {lowerValue.Item2}";

      double finalHigher;

      if (!higher.HasValue)
      {
        finalHigher = maxCapacity;

        model.Warnings.Add(
            GeneralWarnings.DefaultCapacityHighLimit(
                model.StartLineNumber,
                $"{commandNumber} {mnemonic}",
                $"{finalHigher} {model.CapacityUnit}"));
      }
      else
      {
        finalHigher = higher.Value;
      }

      var higherValue = UnitsConvertor.TryConvertBack(finalHigher, unit);
      model.HigherLimitCapacity = higherValue.Item1;
      model.HigherLimitCapacitySource = $"{higherValue.Item1} {higherValue.Item2}";
    
      return model;
    }

    private static void AddLowerTooSmallError(IeCommandModel model, int numberLine, string commandNumber, string mnemonic, double lowerValue, string unit,
    (double MinCapacity, double MaxCapacity, double LowerLimit, double HigherLimit, string DefaultUnit) limits)
    {
      var lower = UnitsConvertor.TryConvertBack(lowerValue, unit);
      var limitsLowerValue = UnitsConvertor.TryConvertBack(limits.LowerLimit, limits.DefaultUnit);
      LogWarning(
          $"В команде {commandNumber} {mnemonic} (строка {numberLine}) " +
          $"нижняя граница электрической емкости меньше минимально измеряемой ({limits.LowerLimit} {limits.DefaultUnit}).");

      model.Errors.Add(
          IeErrors.CapacityLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Нижняя граница электрической емкости ({lower.Item1} {lower.Item2}) " +
              $"меньше минимально измеряемой ({limitsLowerValue.Item1} {limitsLowerValue.Item2})."));
    }

    private static void AddLowerTooLargeError(IeCommandModel model, int numberLine, string commandNumber, string mnemonic, double lowerValue, string unit,
    (double MinCapacity, double MaxCapacity, double LowerLimit, double HigherLimit, string DefaultUnit) limits)
    {
      var lower = UnitsConvertor.TryConvertBack(lowerValue, unit);
      var limitsHigherValue = UnitsConvertor.TryConvertBack(limits.HigherLimit, limits.DefaultUnit);
      LogWarning(
          $"В команде {commandNumber} {mnemonic} (строка {numberLine}) " +
          $"нижняя граница электрической емкости больше максимально возможной ({limits.MaxCapacity}).");

      model.Errors.Add(
          IeErrors.CapacityLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Нижняя граница электрической емкости ({lower.Item1} {lower.Item2}) " +
              $"больше максимально возможной ({limitsHigherValue.Item1} {limitsHigherValue.Item2})."));
    }

    private static void AddHigherTooLargeError(IeCommandModel model, int numberLine, string commandNumber, string mnemonic, double higherValue, string unit,
    (double MinCapacity, double MaxCapacity, double LowerLimit, double HigherLimit, string DefaultUnit) limits)
    {
      var higher = UnitsConvertor.TryConvertBack(higherValue, unit);
      var limitsHigherValue = UnitsConvertor.TryConvertBack(limits.HigherLimit, limits.DefaultUnit);
      LogWarning(
          $"В команде {commandNumber} {mnemonic} (строка {numberLine}) " +
          $"верхняя граница электрической емкости больше максимально возможной ({limits.MaxCapacity}).");

      model.Errors.Add(
          IeErrors.CapacityLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Верхняя граница электрической емкости ({higher.Item1} {higher.Item2}) " +
              $"больше максимально возможной ({limitsHigherValue.Item1} {limitsHigherValue.Item2})."));
    }
    private static void AddHigherTooSmallError(IeCommandModel model, int numberLine, string commandNumber, string mnemonic, double higherValue, string unit,
    (double MinCapacity, double MaxCapacity, double LowerLimit, double HigherLimit, string DefaultUnit) limits)
    {
      var higher = UnitsConvertor.TryConvertBack(higherValue, unit);
      var limitsLowerValue = UnitsConvertor.TryConvertBack(limits.LowerLimit, limits.DefaultUnit);
      LogWarning(
          $"В команде {commandNumber} {mnemonic} (строка {numberLine}) " +
          $"верхняя граница электрической емкости меньше минимально измеряемой ({limits.LowerLimit} {limits.DefaultUnit}).");

      model.Errors.Add(
          IeErrors.CapacityLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Верхняя граница электрической емкости ({higher.Item1} {higher.Item2}) " +
              $"меньше минимально измеряемой ({limitsLowerValue.Item1} {limitsLowerValue.Item2})."));
    }
  }
}
