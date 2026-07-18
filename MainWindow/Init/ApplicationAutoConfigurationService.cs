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
  private static Task ApplyDefaultExecutionSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.CompletedTask;
  }

  /// <summary>
  /// Применяет стандартные настройки протокола.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения настроек протокола.</returns>
  private static Task ApplyDefaultProtocolSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.CompletedTask;
  }

  /// <summary>
  /// Применяет стандартные настройки интерфейса пользователя.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения настроек интерфейса пользователя.</returns>
  private static Task ApplyDefaultInterfaceSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return Task.CompletedTask;
  }
}
