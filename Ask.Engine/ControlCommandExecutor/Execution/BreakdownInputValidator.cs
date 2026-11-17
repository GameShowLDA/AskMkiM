using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandExecutor.Execution;

/// <summary>
/// Проверяет напряжение ППУ по конфигурации без обращения к оборудованию.
/// </summary>
internal static class BreakdownInputValidator
{
  internal static void ValidateMeasurement(IBreakdownTester tester, MeasurementTypeCommand mode,
    DataModel data, bool hasVoltageField)
  {
    // В метрологии ПИ напряжение задаётся основным параметром, в тестах — отдельным полем.
    ValidateVoltage(tester, mode, hasVoltageField ? data.Voltage : data.Param);
  }

  internal static void ValidateCommand(IBreakdownTester tester, BaseCommandModel command)
  {
    switch (command)
    {
      case SiCommandModel si:
        ValidateVoltage(tester, MeasurementTypeCommand.SI, si.Voltage ?? double.NaN);
        break;
      case PiCommandModel pi:
        ValidateVoltage(tester, pi.VoltageType == VoltageEnum.Type.ACW
          ? MeasurementTypeCommand.PI_ACW : MeasurementTypeCommand.PI_DCW, pi.Voltage ?? double.NaN);
        if (pi.SiCommand != null)
        {
          if (pi.SiCommand.Errors.Count > 0)
            throw new InputValidationException(pi.SiCommand.Errors[0]);
          ValidateCommand(tester, pi.SiCommand);
        }
        break;
    }
  }

  internal static void ValidateVoltage(IBreakdownTester tester, MeasurementTypeCommand mode, double voltage)
  {
    var range = mode switch
    {
      MeasurementTypeCommand.PI_ACW => tester.AcwManger.VoltageRange,
      MeasurementTypeCommand.PI_DCW => tester.DcwManger.VoltageRange,
      MeasurementTypeCommand.SI => tester.IrManger.VoltageRange,
      _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    if (range.IsAllowed(voltage))
      return;

    var exceptions = range.Exceptions.Count > 0
      ? $"; допустимые исключения: {string.Join(", ", range.Exceptions)} В"
      : string.Empty;
    throw new InputValidationException(new ErrorItem
    {
      Code = ErrorCode.Metrology_Validation_InvalidVoltage,
      Description = $"Недопустимое напряжение {voltage} В для {mode}: " +
        $"диапазон {range.MinVoltage}–{range.MaxVoltage} В, шаг {range.Step} В{exceptions}."
    });
  }
}
