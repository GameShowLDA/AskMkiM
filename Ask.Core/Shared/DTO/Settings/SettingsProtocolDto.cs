namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO настроек формирования протокола.
/// Содержит параметры отображения и шаблоны текстов протокола.
/// </summary>
public class SettingsProtocolDto
{
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
}
