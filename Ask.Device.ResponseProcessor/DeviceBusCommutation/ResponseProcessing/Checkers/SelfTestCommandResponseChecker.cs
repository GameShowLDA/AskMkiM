using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;

namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет ответы команд самоконтроля УКШ.
/// </summary>
internal static class SelfTestCommandResponseChecker
{
  /// <summary>
  /// Проверяет подтверждение управления отдельным реле цепи самоконтроля.
  /// </summary>
  /// <param name="response">Числовой ответ УКШ.</param>
  /// <returns>
  /// <see langword="true"/>, если УКШ подтвердил выполнение команды.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool CheckRelayControl(string response)
    => NumericCommandResponseChecker.Check(response, 1);

  /// <summary>
  /// Проверяет подтверждение коммутации цепи самоконтроля.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="connectorType">Тип проверяемой цепи.</param>
  /// <param name="busContact">Контакт шины.</param>
  /// <param name="action">Код действия над цепью.</param>
  /// <returns>
  /// <see langword="true"/>, если ответ соответствует отправленной команде.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  internal static bool CheckCircuitControl(
    string response,
    ISwitchingDevice device,
    int connectorType,
    int busContact,
    int action)
    => EquipmentCommandResponseChecker.Check(
      response, device, 4, connectorType, busContact, action);
}
