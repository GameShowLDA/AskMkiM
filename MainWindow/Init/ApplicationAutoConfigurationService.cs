using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.UiEnums;

namespace MainWindowProgram.Init;

/// <summary>
/// Применяет стандартные настройки приложения и создаёт начальную конфигурацию оборудования.
/// </summary>
internal sealed class ApplicationAutoConfigurationService
{
  /// <summary>
  /// Применяет стандартные настройки приложения по отдельным разделам.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения стандартных настроек.</returns>
  public async Task ApplyDefaultConfigurationAsync(CancellationToken cancellationToken = default)
  {
    await ApplyDefaultEquipmentConfigurationAsync(cancellationToken);
    await ApplyDefaultExecutionSettingsAsync(cancellationToken);
    await ApplyDefaultProtocolSettingsAsync(cancellationToken);
    await ApplyDefaultInterfaceSettingsAsync(cancellationToken);
  }

  /// <summary>
  /// Применяет стандартную конфигурацию оборудования.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения конфигурации оборудования.</returns>
  /// <remarks>
  /// Конкретный состав оборудования должен быть определён отдельно, поскольку он
  /// зависит от подключённого оборудования и не может быть представлен единым
  /// универсальным значением по умолчанию.
  /// </remarks>
  private static Task ApplyDefaultEquipmentConfigurationAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.CompletedTask;
  }

  /// <summary>
  /// Применяет стандартные настройки выполнения.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения настроек выполнения.</returns>
  private static async Task ApplyDefaultExecutionSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var defaultSettings = new SettingsExecutionDto
    {
      IdleModeExecution = false,
      IsErrorSimulationMode = false,
      StepByStepMode = false,
      StopOnError = false,
      LegacyCompatibilityMode = false
    };

    await ExecutionConfig.SaveExecutionModel(defaultSettings);
  }

  /// <summary>
  /// Применяет стандартные настройки протокола.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения настроек протокола.</returns>
  private static async Task ApplyDefaultProtocolSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    // Берём уже загруженную модель, чтобы не затереть шаблоны протоколов пустыми строками.
    var defaultSettings = ProtocolConfig.GetProtocolModel();
    defaultSettings.ShowDeviceInfo = true;
    defaultSettings.ShowHeaderInfo = true;
    defaultSettings.AutoSaveProtocol = true;
    defaultSettings.AutoPrintProtocol = true;
    defaultSettings.DisplayOperationTime = true;
    defaultSettings.ShowDetailedProtocol = true;
    defaultSettings.ShowProtocolInSoftware = true;
    defaultSettings.GenerateProtocol = true;
    defaultSettings.ShowCommandHeadersInProtocol = true;
    defaultSettings.ShowTestStepMessagesInProtocol = true;

    await ProtocolConfig.SaveProtocolModel(defaultSettings);
  }

  /// <summary>
  /// Применяет стандартные настройки интерфейса пользователя.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения настроек интерфейса пользователя.</returns>
  private static async Task ApplyDefaultInterfaceSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var defaultInterfaceSettings = await UserInterfaceConfig.GetParameterModel();
    defaultInterfaceSettings.Language = "ru";
    defaultInterfaceSettings.Theme = ThemeMode.DarkCustom;
    defaultInterfaceSettings.UseSyntaxHighlighting = true;
    defaultInterfaceSettings.UseCommandBodyBackgroundHighlighting = true;
    defaultInterfaceSettings.UseChainPointBodyBackgroundHighlighting = true;

    await UserInterfaceConfig.SaveProtocolModel(defaultInterfaceSettings);

    var defaultProtocolSettings = ProtocolConfig.GetProtocolModel();
    defaultProtocolSettings.PrintFontFamily = "Consolas";
    defaultProtocolSettings.PrintFontSize = 16;

    await ProtocolConfig.SaveProtocolModel(defaultProtocolSettings);
  }
}
