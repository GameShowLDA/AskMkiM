using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;

namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет подтверждение коммутации оборудования и вспомогательных реле УКШ.
/// </summary>
internal static class EquipmentCommandResponseChecker
{
  /// <summary>
  /// Проверяет адрес УКШ и точное подтверждение команды с тремя параметрами.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="commandNumber">Номер команды прошивки.</param>
  /// <param name="firstParameter">Первый параметр команды.</param>
  /// <param name="secondParameter">Второй параметр команды.</param>
  /// <param name="action">Код действия команды.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ соответствует отправленной команде.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(
    string response,
    ISwitchingDevice device,
    int commandNumber,
    int firstParameter,
    int secondParameter,
    int action)
    => JsonCommandResponseChecker.Check(
      response,
      device,
      $"{commandNumber}.{firstParameter}.{secondParameter}.{action}");
}
