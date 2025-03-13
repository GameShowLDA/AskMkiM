using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base;
using NewCore.Interface;

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
