using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply
{
  /// <summary>
  /// Предоставляет управление программируемым источником постоянного напряжения и тока.
  /// </summary>
  public interface IPowerSupply : IAttachableDevice, IDeviceToDtoConverter<PowerSupplyDto>
  {
    /// <summary>
    /// Управление уставкой напряжения.
    /// </summary>
    IVoltageManager VoltageManager { get; set; }

    /// <summary>
    /// Управление уставкой ограничения тока.
    /// </summary>
    ICurrentManager CurrentManager { get; set; }

    /// <summary>
    /// Управление состоянием выхода.
    /// </summary>
    IOutputManager OutputManager { get; set; }

    /// <summary>
    /// Измерение выходного напряжения и тока.
    /// </summary>
    IMeasurementManager MeasurementManager { get; set; }

    /// <summary>
    /// Менеджер самоконтроля источника питания.
    /// </summary>
    ISelfTestCheckerPowerSupply SelfTestManager { get; set; }
  }
}
