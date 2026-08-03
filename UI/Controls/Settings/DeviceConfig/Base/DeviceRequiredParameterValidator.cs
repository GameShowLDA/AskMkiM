using System.Globalization;

namespace UI.Controls.Settings.DeviceConfig.Base;

/// <summary>
/// Проверяет обязательные параметры в формах конфигурации устройств.
/// </summary>
public static class DeviceRequiredParameterValidator
{
  /// <summary>
  /// Возвращает поддерживаемый тип подключения для доступного пункта списка.
  /// </summary>
  /// <param name="content">Текст пункта списка типов подключения.</param>
  /// <param name="isEnabled">Признак доступности пункта списка.</param>
  /// <returns>Тип подключения или пустая строка, если пункт недоступен либо не поддерживается.</returns>
  public static string NormalizeConnectionType(string? content, bool isEnabled)
  {
    if (!isEnabled)
    {
      return string.Empty;
    }

    string text = content?.Trim() ?? string.Empty;
    return text is "IP" or "COM" or "USB" ? text : string.Empty;
  }

  /// <summary>
  /// Проверяет значение октета IPv4-адреса.
  /// </summary>
  /// <param name="value">Значение октета.</param>
  /// <returns>
  /// <see langword="true"/>, если значение находится в диапазоне от 0 до 255.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsValidIpPart(int value) => value is >= 0 and <= 255;

  /// <summary>
  /// Проверяет значения всех октетов IPv4-адреса.
  /// </summary>
  /// <param name="part1">Первый октет IPv4-адреса.</param>
  /// <param name="part2">Второй октет IPv4-адреса.</param>
  /// <param name="part3">Третий октет IPv4-адреса.</param>
  /// <param name="part4">Четвёртый октет IPv4-адреса.</param>
  /// <returns>
  /// <see langword="true"/>, если все октеты находятся в диапазоне от 0 до 255.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsValidIpAddress(int part1, int part2, int part3, int part4)
  {
    return IsValidIpPart(part1) &&
      IsValidIpPart(part2) &&
      IsValidIpPart(part3) &&
      IsValidIpPart(part4);
  }

  /// <summary>
  /// Проверяет строковое представление неотрицательного числа.
  /// </summary>
  /// <param name="text">Строковое представление числа.</param>
  /// <returns>
  /// <see langword="true"/>, если строка содержит число, большее или равное нулю.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsNonNegativeNumber(string? text)
  {
    return TryGetNumber(text, out double value) && value >= 0;
  }

  /// <summary>
  /// Проверяет строковое представление положительного числа.
  /// </summary>
  /// <param name="text">Строковое представление числа.</param>
  /// <returns>
  /// <see langword="true"/>, если строка содержит число больше нуля.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsPositiveNumber(string? text)
  {
    return TryGetNumber(text, out double value) && value > 0;
  }

  /// <summary>
  /// Преобразует строку с точкой или запятой в число.
  /// </summary>
  /// <param name="text">Строковое представление числа.</param>
  /// <param name="value">Преобразованное число при успешном разборе.</param>
  /// <returns>
  /// <see langword="true"/>, если строка успешно преобразована.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  private static bool TryGetNumber(string? text, out double value)
  {
    return double.TryParse(
      text?.Trim().Replace(',', '.'),
      NumberStyles.AllowDecimalPoint,
      CultureInfo.InvariantCulture,
      out value);
  }
}
