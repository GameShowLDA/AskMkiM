using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Settings.DeviceConfig.Base.BaseSettings
{
  /// <summary>
  /// Часть класса, отвечающая за объявление событий.
  /// Позволяет подписываться на события, связанные с управлением интерфейсом настроек шасси.
  /// </summary>
  public partial class BaseSettingsControl
  {
    /// <summary>
    /// Событие, вызываемое при запросе на закрытие окна настроек.
    /// Может использоваться для обработки логики отмены или сохранения данных перед закрытием.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при запросе на сохранение настроек.
    /// Может использоваться для обработки логики отмены или сохранения данных перед закрытием.
    /// </summary>
    public event EventHandler RequestSave;
  }
}
