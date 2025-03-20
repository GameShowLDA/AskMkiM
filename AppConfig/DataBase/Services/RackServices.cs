using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfig.DataBase.Models;
using NewCore.Base.Interface.Main;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Сервис для работы с моделями стоек СКМ из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class RackServices : Service<IRack>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RackServices"/>.
    /// </summary>
    public RackServices() : base(AppConfig.Config.SystemStateManager.Context)
    { }
  }
}
