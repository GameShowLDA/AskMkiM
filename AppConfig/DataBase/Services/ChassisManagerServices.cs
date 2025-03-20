using NewCore.Base.Interface.Main;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Сервис для работы с моделями мененджера шасси из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class ChassisManagerServices : Service<IChassisManager>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ChassisManagerServices"/>.
    /// </summary>
    public ChassisManagerServices() : base(AppConfig.Config.SystemStateManager.Context)
    { }
  }
}
