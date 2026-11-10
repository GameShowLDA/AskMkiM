using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.ResponseProcessor.PowerSupply;

/// <summary>
/// Публикует сообщения самоконтроля внешнего источника питания.
/// </summary>
public static class PowerSupplySelfTestMessages
{
  public static Task PublishTitleAsync(IPowerSupply device, IMessageOutputService outputService)
    => EquipmentMessages.PublishDeviceHealthCheckTitleAsync(device, outputService);

  public static Task PublishInformationAsync(string header, IMessageOutputService outputService)
    => SelfTestMessages.PublishInformationAsync(header, outputService);

  public static Task PublishVoltagePointAsync(double target, IMessageOutputService outputService)
    => PublishPointAsync("напряжения", target, VoltageUnit.Volt, outputService);

  public static Task PublishCurrentPointAsync(double target, IMessageOutputService outputService)
    => PublishPointAsync("тока", target, CurrentUnit.Ampere, outputService);

  public static Task PublishVoltageResultAsync(MeasurementRange expected, double measured,
    bool passed, string? failure, IMessageOutputService outputService)
    => PublishResultAsync(expected, measured, VoltageUnit.Volt, passed, failure, outputService);

  public static Task PublishCurrentResultAsync(MeasurementRange expected, double measured,
    bool passed, string? failure, IMessageOutputService outputService)
    => PublishResultAsync(expected, measured, CurrentUnit.Ampere, passed, failure, outputService);

  public static string DescribeVoltageFailure(MeasurementRange expected, double measured)
    => DescribeFailure("напряжения", expected, measured, VoltageUnit.Volt);

  public static string DescribeCurrentFailure(MeasurementRange expected, double measured)
    => DescribeFailure("тока", expected, measured, CurrentUnit.Ampere);

  private static async Task PublishPointAsync(string quantity, double target, Enum unit, IMessageOutputService outputService)
  {
    await outputService.AppendEmptyLineAsync();
    await SelfTestMessages.PublishInformationAsync(
      $"Проверка {quantity} при уставке {Format(target, unit)}", outputService, indentLevel: 1);
  }

  private static async Task PublishResultAsync(MeasurementRange expected, double measured,
    Enum unit, bool passed, string? failure, IMessageOutputService outputService)
  {
    await MeasurementMessages.PublishResultAsync(CheckType.SelfTest, unit,
      new MeasurementRange(measured, expected.LowerBound, expected.UpperBound), passed,
      measurementTarget: $"Уставка {Format(expected.TargetValue, unit)}",
      executionErrorMessage: failure, outputService: outputService);
  }

  private static string DescribeFailure(string quantity, MeasurementRange expected, double measured, Enum unit)
    => $"Проверка {quantity} при уставке {Format(expected.TargetValue, unit)}. " +
      $"Измерено {Format(measured, unit)}. " +
      $"Допустимый диапазон от {Format(expected.LowerBound, unit)} до {Format(expected.UpperBound, unit)}.";

  private static string Format(double value, Enum unit)
    => MeasurementValueFormatter.FormatWithUnit(value, unit.GetUnit());
}
