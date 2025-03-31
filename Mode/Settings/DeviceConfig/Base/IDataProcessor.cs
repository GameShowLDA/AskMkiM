using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base.Device;

namespace Mode.Settings.DeviceConfig.Base
{
  public interface IDataProcessor
  {
    /// <summary>
    /// Выполняет обработку и заполнение специфичных данных устройства.
    /// </summary>
    /// <param name="device">Модель устройства.</param>
    /// <param name="control">Элемент управления, из которого извлекаются данные.</param>
    void ProcessData(IDevice device, DeviceSettingsControl control);
  }
}
