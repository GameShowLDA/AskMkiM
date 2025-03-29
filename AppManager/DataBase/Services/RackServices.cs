using NewCore.Base.Interface.Main;

namespace AppManager.DataBase.Services
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
    public RackServices() : base(AppManager.Config.SystemStateManager.Context)
    { }
  }
}
