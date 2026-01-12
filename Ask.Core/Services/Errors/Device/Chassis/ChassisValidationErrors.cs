using Ask.Core.Services.Errors.Models;

namespace Ask.Core.Services.Errors.Device.Chassis
{
  /// <summary>
  /// Содержит стандартные ошибки, возникающие при проверке шасси —
  /// его наличия, адреса, корректности подключения и структуры модулей.
  /// </summary>
  public static class ChassisValidationErrors
  {
    public static SystemExceptionBase NotFound(int number) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Equipment_ChassisNotFound,
        Description = $"Шасси с номером {number} не найдено в конфигурации."
      });
  }
}
