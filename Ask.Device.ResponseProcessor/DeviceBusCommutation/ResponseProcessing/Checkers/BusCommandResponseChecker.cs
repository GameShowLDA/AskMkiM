using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;

namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет подтверждение подключения или отключения всех шин УКШ.
/// </summary>
internal static class BusCommandResponseChecker
{
  /// <summary>
  /// Проверяет адрес УКШ и подтверждение требуемого состояния всех шин.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="connect">Ожидаемое действие подключения шин.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ соответствует отправленной команде.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool Check(string response, ISwitchingDevice device, bool connect)
    => JsonCommandResponseChecker.Check(response, device, $"7.{(connect ? 1 : 2)}");
}
