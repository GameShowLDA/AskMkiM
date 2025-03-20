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
  /// Сервис для работы с моделями модуля источника напряжения и тока из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class PowerSourceModuleServices : Service<IPowerSourceModule>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PowerSourceModuleServices"/>.
    /// </summary>
    public PowerSourceModuleServices() : base(AppConfig.Config.SystemStateManager.Context)
    { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список пробойных установок.</returns>
    public List<IPowerSourceModule> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = new PowerSourceModuleRepository(_context).GetDevicesByNumberChassis(numberChassis);
      var result = data
      .OfType<IDevice>()
      .Select(GetDeviceInstance)
      .Where(instance => instance != null)
      .ToList();

      return result;
    }
  }
}
