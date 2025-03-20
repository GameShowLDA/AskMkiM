using AppConfig.DataBase.Repositories;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Сервис для работы с моделями точных измерителей из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class PrecisionMeterServices : Service<IPrecisionMeter>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PrecisionMeterServices"/>.
    /// </summary>
    public PrecisionMeterServices() : base(AppConfig.Config.SystemStateManager.Context)
    { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список пробойных установок.</returns>
    public List<IPrecisionMeter> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = new PrecisionMeterRepository().GetDevicesByNumberChassis(numberChassis);
      var result = data
      .OfType<IDevice>()
      .Select(GetDeviceInstance)
      .Where(instance => instance != null)
      .ToList();

      return result;
    }
  }
}
