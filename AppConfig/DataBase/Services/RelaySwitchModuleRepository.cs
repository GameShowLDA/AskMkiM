using AppConfig.DataBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления модулями коммутации реле
  /// </summary>
  public class RelaySwitchModuleRepository : Repository<RelaySwitchModuleEntity>
  {
    public RelaySwitchModuleRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси</param>
    /// <returns>Список модулей коммутации реле.</returns>
    public List<RelaySwitchModuleEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
