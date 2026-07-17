namespace Ask.Core.Shared.DTO.Devices.Base;

/// <summary>
/// Содержит параметры последовательного порта,
/// используемые для подключения устройства по интерфейсу COM.
/// </summary>
public sealed class ComPortSettings
{
  /// <summary>
  /// Имя последовательного порта.
  /// </summary>
  public string PortName { get; init; } = string.Empty;

  /// <summary>
  /// Скорость обмена данными (бод).
  /// </summary>
  public int BaudRate { get; init; } = 9600;

  /// <summary>
  /// Режим контроля чётности.
  /// </summary>
  public string Parity { get; init; } = "None";

  /// <summary>
  /// Количество информационных битов в символе.
  /// </summary>
  public int DataBits { get; init; } = 8;

  /// <summary>
  /// Количество стоп-битов.
  /// </summary>
  public string StopBits { get; init; } = "One";

  /// <summary>
  /// Режим управления потоком данных.
  /// </summary>
  public string Handshake { get; init; } = "None";

  /// <summary>
  /// Имя кодировки, используемой при обмене текстовыми данными.
  /// </summary>
  public string EncodingName { get; init; } = "us-ascii";
}