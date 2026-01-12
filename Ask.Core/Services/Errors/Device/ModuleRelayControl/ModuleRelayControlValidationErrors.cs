using Ask.Core.Services.Errors.Models;

namespace Ask.Core.Services.Errors.Device.ModuleRelayControl
{
  /// <summary>
  /// Содержит стандартные ошибки, возникающие при проверке модулей коммутации (МКР)
  /// — их наличия, корректности конфигурации и диапазона точек.
  /// </summary>
  public static class ModuleRelayControlValidationErrors
  {
    /// <summary>
    /// Исключение: модуль коммутации с указанным номером не найден в заданном шасси.
    /// </summary>
    /// <param name="chassisNumber">Номер шасси.</param>
    /// <param name="moduleNumber">Номер модуля.</param>
    /// <returns>Экземпляр <see cref="SystemExceptionBase"/>.</returns>
    public static SystemExceptionBase ModuleNotFound(int chassisNumber, int moduleNumber) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Equipment_ModuleNotFound,
        Description = $"Модуль {moduleNumber} в шасси {chassisNumber} не найден."
      });

    /// <summary>
    /// Исключение: точка выходит за пределы диапазона, поддерживаемого модулем коммутации.
    /// </summary>
    /// <param name="chassisNumber">Номер шасси.</param>
    /// <param name="moduleNumber">Номер модуля.</param>
    /// <param name="point">Номер точки.</param>
    /// <param name="maxPoint">Максимально допустимое количество точек.</param>
    public static SystemExceptionBase PointOutOfRange(int chassisNumber, int moduleNumber, int point, int maxPoint) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Equipment_PointOutOfRange,
        Description = $"Точка {point} в модуле {moduleNumber} шасси {chassisNumber} выходит за предел диапазона (1–{maxPoint})."
      });
  }
}
