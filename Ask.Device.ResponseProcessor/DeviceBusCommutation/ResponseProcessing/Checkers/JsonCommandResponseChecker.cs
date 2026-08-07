using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseModels;
using System.Text.Json;

namespace Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет адрес и подтверждение команды в JSON-ответе УКШ.
/// </summary>
internal static class JsonCommandResponseChecker
{
  /// <summary>
  /// Имя модуля УКШ в JSON-ответах прошивки.
  /// </summary>
  private const string ModuleName = "DeviceBusCommutation";

  /// <summary>
  /// Проверяет имя, адрес отправителя и ожидаемое подтверждение команды.
  /// </summary>
  /// <param name="response">JSON-ответ УКШ.</param>
  /// <param name="device">УКШ, которому была отправлена команда.</param>
  /// <param name="expectedAnswer">
  /// Ожидаемое значение поля <c>Answer</c> или <see langword="null"/>, если поле проверять не требуется.
  /// </param>
  /// <returns>
  /// <see langword="true"/>, если ответ принадлежит ожидаемому УКШ и содержит требуемое подтверждение.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool Check(string response, ISwitchingDevice device, string? expectedAnswer = null)
  {
    if (string.IsNullOrWhiteSpace(response))
    {
      return false;
    }

    try
    {
      DeviceBusCommutationResponse? model = JsonSerializer.Deserialize<DeviceBusCommutationResponse>(response);
      return model != null &&
        model.ModuleName == ModuleName &&
        model.NumberChassis == device.NumberChassis &&
        model.NumberDevice == device.Number &&
        (expectedAnswer == null || model.Answer == expectedAnswer);
    }
    catch (JsonException)
    {
      return false;
    }
  }
}
