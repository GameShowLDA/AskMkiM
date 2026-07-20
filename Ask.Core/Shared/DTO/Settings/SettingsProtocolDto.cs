using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO настроек формирования протокола.
/// Содержит параметры отображения и шаблоны текстов протокола.
/// </summary>
[Table("SettingsProtocol")]
public class SettingsProtocolDto
{
  /// <summary>
  /// Идентификатор записи настроек.
  /// </summary>
  [Key]
  public int Id { get; set; }

  /// <summary>
  /// Отображать информацию об устройстве в протоколе.
  /// </summary>
  public bool ShowDeviceInfo { get; set; }

  /// <summary>
  /// Отображать заголовочную информацию при выполнении.
  /// </summary>
  public bool ShowHeaderInfo { get; set; }

  /// <summary>
  /// Автоматически сохранять протокол.
  /// </summary>
  public bool AutoSaveProtocol { get; set; }

  /// <summary>
  /// Автоматически печатать протокол.
  /// </summary>
  public bool AutoPrintProtocol { get; set; }

  /// <summary>
  /// Отображать время выполнения операций.
  /// </summary>
  public bool DisplayOperationTime { get; set; }

  /// <summary>
  /// Включать подробное отображение протокола.
  /// </summary>
  public bool ShowDetailedProtocol { get; set; }

  /// <summary>
  /// Отображать протокол в программном обеспечении.
  /// </summary>
  public bool ShowProtocolInSoftware { get; set; }

  /// <summary>
  /// Формировать протокол выполнения.
  /// </summary>
  public bool GenerateProtocol { get; set; }

  /// <summary>
  /// Шаблон протокола без ошибок.
  /// </summary>
  public string CleanTextProtocol { get; set; } = string.Empty;

  /// <summary>
  /// Шаблон протокола при наличии ошибок.
  /// </summary>
  public string CleanTextErrorsProtocol { get; set; } = string.Empty;

  /// <summary>
  /// Текст протокола с ошибками (формируемый или пользовательский).
  /// </summary>
  public string ErrorTextProtocol { get; set; } = string.Empty;

  /// <summary>
  /// Получает или задаёт значение, указывающее, отображаются ли заголовки команд в протоколе.
  /// </summary>
  public bool ShowCommandHeadersInProtocol { get; set; }

  /// <summary>
  /// Получает или задаёт значение, указывающее, отображаются ли шаги проверки в протоколе.
  /// </summary>
  public bool ShowTestStepMessagesInProtocol { get; set; }

  public string PrintFontFamily { get; set; } = "Consolas";

  public double PrintFontSize { get; set; } = 10;
}
