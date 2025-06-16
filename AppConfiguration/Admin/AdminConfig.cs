using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Base;

namespace AppConfiguration.Admin
{
  static public class AdminConfig
  {
    /// <summary>
    /// Флаг, указывающий, запущено ли приложение с правами администратора.
    /// </summary>
    static internal bool IsAdmin { get; set; }

    /// <summary>
    /// Флаг, указывающий перехват Exception в режиме админа.
    /// </summary>
    static public bool ErrorDebug { get; set; } = false;

    /// <summary>
    /// Устанавливает статус прав администратора.
    /// </summary>
    /// <param name="enable">true, если запущено с правами администратора; false в противном случае.</param>
    static public async Task SetAdminRights(bool enable)
    {
      await Task.Run(() =>
      {
        EventAggregator.AdminRightsFlag = enable;
      });
    }

    /// <summary>
    /// Возвращает текущий статус прав администратора.
    /// </summary>
    /// <returns>true, если запущено с правами администратора; false в противном случае.</returns>
    static public async Task<bool> GetAdminRights() => await Task.Run(() => IsAdmin);
  }
}
