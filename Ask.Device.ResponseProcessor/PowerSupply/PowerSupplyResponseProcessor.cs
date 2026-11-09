using Ask.Core.Services.Errors.Device;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;
using System.Globalization;

namespace Ask.Device.ResponseProcessor.PowerSupply;

/// <summary>
/// Проверяет ответы внешних источников питания SCPI.
/// </summary>
public static class PowerSupplyResponseProcessor
{
  /// <summary>
  /// Проверяет модель источника в ответе идентификации.
  /// </summary>
  /// <param name="response">Ответ на запрос идентификации.</param>
  /// <param name="model">Ожидаемая модель источника.</param>
  /// <returns>Признак совпадения модели.</returns>
  public static bool CheckInitialization(string response, string model)
  {
    string[] fields = response.Split(',');
    return fields.Length == 4 && fields[1].Trim().Equals(model, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Читает числовое значение из ответа источника.
  /// </summary>
  /// <param name="response">Ответ источника.</param>
  /// <returns>Числовое значение ответа.</returns>
  public static decimal ParseNumber(string response)
    => decimal.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
      ? value : throw new DeviceException($"Некорректный числовой ответ источника питания: {response}");

  /// <summary>
  /// Проверяет отсутствие ошибки SCPI.
  /// </summary>
  /// <param name="response">Ответ на запрос системной ошибки.</param>
  public static void EnsureNoError(string response)
  {
    if (!MultimeterResponseProcessor.CheckNoInstrumentError(response, out _))
      throw new DeviceException($"Ошибка источника питания: {response}");
  }

  /// <summary>
  /// Проверяет совпадение заданной и прочитанной уставки.
  /// </summary>
  /// <param name="response">Ответ чтения уставки.</param>
  /// <param name="expected">Ожидаемая уставка.</param>
  public static void EnsureSetpoint(string response, decimal expected)
  {
    decimal actual = ParseNumber(response);
    if (actual != expected)
      throw new DeviceException($"Источник питания не подтвердил уставку: задано {expected}, прочитано {actual}.");
  }

  /// <summary>
  /// Читает состояние выхода из ответа источника.
  /// </summary>
  /// <param name="response">Ответ на запрос состояния выхода.</param>
  /// <returns>Признак включённого выхода.</returns>
  public static bool ParseOutputState(string response) => ParseNumber(response) switch
  {
    0 => false,
    1 => true,
    _ => throw new DeviceException($"Некорректное состояние выхода источника питания: {response}"),
  };
}
