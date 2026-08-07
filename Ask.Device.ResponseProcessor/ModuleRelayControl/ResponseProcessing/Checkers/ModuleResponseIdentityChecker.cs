using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет принадлежность ответа ожидаемому МКР.
/// </summary>
internal static class ModuleResponseIdentityChecker
{
  private const string ModuleName = "MKR";

  /// <summary>
  /// Проверяет имя и адрес МКР в полученном ответе.
  /// </summary>
  /// <param name="response">Десериализованный ответ МКР.</param>
  /// <param name="chassisNumber">Ожидаемый номер шасси.</param>
  /// <param name="moduleNumber">Ожидаемый номер МКР в шасси.</param>
  /// <returns>
  /// <see langword="true"/>, если имя и адрес отправителя совпадают с ожидаемыми.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(
    ModuleRelayControlResponse response,
    int chassisNumber,
    int moduleNumber)
  {
    return string.Equals(response.ModuleName, ModuleName, StringComparison.OrdinalIgnoreCase) &&
      response.NumberChassis == chassisNumber &&
      response.NumberDevice == moduleNumber;
  }
}
