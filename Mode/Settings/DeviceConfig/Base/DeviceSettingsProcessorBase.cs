using DataBaseConfiguration.Models;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.Base
{
  /// <summary>
  /// Базовый класс для обработки настроек устройств и создания соответствующих моделей.
  /// </summary>
  public class DeviceSettingsProcessorBase
  {
    /// <summary>
    /// Метод создания и обработки модели устройства.
    /// </summary>
    /// <param name="selectedDevice">Интерфейс выбранного устройства.</param>
    /// <param name="control">Элемент управления с настройками.</param>
    /// <param name="additionalDataProcessor">Внешний обработчик специфичных данных.</param>
    /// <returns>Заполненная модель устройства (реализующая интерфейс IDevice).</returns>
    public T ProcessDevice<T>(
        IDevice selectedDevice,
        DeviceSettingsControl control,
        IDataProcessor? additionalDataProcessor = null)
      where T : class, IDevice
    {
      string connectString = BaseHandler<IDevice>.GetConnectionDetails(control, selectedDevice);

      var deviceModel = CreateDeviceModelByInterface(selectedDevice) as T;

      if (deviceModel is null)
      {
        throw new ArgumentNullException(nameof(deviceModel));
      }

      deviceModel.Name = selectedDevice.Name;
      deviceModel.Description = selectedDevice.Description;
      deviceModel.ConnectionDetails = connectString;
      deviceModel.Number = BaseHandler<IDevice>.GetNumber(control);
      deviceModel.DeviceClass = selectedDevice.DeviceClass;
      SetChassisNumber(deviceModel, control);

      return deviceModel;
    }

    /// <summary>
    /// Создание конкретной модели по интерфейсу устройства.
    /// </summary>
    /// <param name="device">Выбранное устройство.</param>
    /// <returns>Возвращает созданный экземпляр выбранного устройства.</returns>
    protected IDevice CreateDeviceModelByInterface(IDevice device)
    {
      return device switch
      {
        IBreakdownTester => new BreakdownTesterEntity(),
        IPowerSourceModule => new PowerSourceModuleEntity(),
        IPrecisionMeter => new PrecisionMeterEntity(),
        IRelaySwitchModule => new RelaySwitchModuleEntity(),
        ISwitchingDevice => new SwitchingDeviceEntity(),
        IChassisManager => new ChassisManagerEntity(),
        IFastMeter => new FastMeterEntity(),
        IRack => new RackEntity(),

        _ => throw new ArgumentException("Неизвестный тип устройства", nameof(device))
      };
    }

    /// <summary>
    /// Определяет номер шасси, если устройство его поддерживает, и выводит в консоль.
    /// </summary>
    /// <param name="deviceModel">Объект устройства.</param>
    private void SetChassisNumber(IDevice deviceModel, DeviceSettingsControl control)
    {
      if (deviceModel.DeviceType != NewCore.Enum.DeviceEnum.DeviceType.ChassisManager)
      {
        IAttachableDevice attachableDevice = (IAttachableDevice)deviceModel;
        attachableDevice.NumberChassis = control.NumberChassis;
      }
    }
  }
}
