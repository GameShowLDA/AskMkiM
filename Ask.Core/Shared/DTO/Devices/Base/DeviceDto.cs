using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.DTO.Devices.Base;

/// <summary>
/// Базовый объект передачи данных для устройства.
/// Содержит общие свойства, используемые всеми типами устройств.
/// </summary>
public class DeviceDto
{
  /// <summary>
  /// Уникальный идентификатор устройства.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  /// Имя устройства для отображения и идентификации.
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Описание устройства, содержащее дополнительную информацию о его назначении.
  /// </summary>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Номер устройства в системе.
  /// </summary>
  public int Number { get; set; }

  /// <summary>
  /// Данные подключения (например, IP-адрес или COM-порт).
  /// </summary>
  public string ConnectionDetails { get; set; } = string.Empty;

  /// <summary>
  /// Тип устройства.
  /// </summary>
  public DeviceType DeviceType { get; set; }

  /// <summary>
  /// Полный путь к классу устройства.
  /// Используется для динамического создания экземпляра.
  /// </summary>
  public string DeviceClass { get; set; } = string.Empty;
}