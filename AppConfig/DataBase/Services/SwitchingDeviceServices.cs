using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Repositories;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Сервис для работы с моделями устройства коммутации шин из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class SwitchingDeviceServices : Service<ISwitchingDevice>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SwitchingDeviceServices"/>.
    /// </summary>
    public SwitchingDeviceServices() : base(AppConfig.Config.SystemStateManager.Context)
    { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список пробойных установок.</returns>
    public List<ISwitchingDevice> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = new SwitchingDeviceRepository().GetDevicesByNumberChassis(numberChassis);
      var result = data
      .OfType<IDevice>()
      .Select(GetDeviceInstance)
      .Where(instance => instance != null)
      .ToList();

      return result;
    }
  }
}
