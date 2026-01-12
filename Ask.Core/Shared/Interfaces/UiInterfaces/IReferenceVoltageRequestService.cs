using System.Windows.Controls;

namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  /// <summary>
  /// Сервис запроса опорного напряжения у пользователя.
  /// Не зависит от конкретной реализации UI.
  /// </summary>
  public interface IReferenceVoltageRequestService
  {
    /// <summary>
    /// Запрашивает у пользователя значение опорного напряжения.
    /// </summary>
    /// <returns>
    /// Введённое значение или null, если пользователь отменил ввод.
    /// </returns>
    Task<double?> RequestReferenceVoltageAsync(UserControl control);
  }
}
