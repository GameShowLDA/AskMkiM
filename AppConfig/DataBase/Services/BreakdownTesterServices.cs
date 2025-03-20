using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Repositories;
using Microsoft.EntityFrameworkCore;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Сервис для работы с моделями ППУ из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class BreakdownTesterServices : Service<IBreakdownTester>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BreakdownTesterServices"/>.
    /// </summary>
    public BreakdownTesterServices() : base(AppConfig.Config.SystemStateManager.Context)
    { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список пробойных установок.</returns>
    public List<IBreakdownTester> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = new BreakdownTesterRepository().GetDevicesByNumberChassis(numberChassis);
      var result = data
      .OfType<IDevice>()
      .Select(GetDeviceInstance)
      .Where(instance => instance != null)
      .ToList();

      return result;
    }
  }
}
