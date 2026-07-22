using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.UI.Controls.ProtocolNew;
using Ask.UI.Features.ProtocolNew.Protocol;
using Ask.UI.Features.ProtocolNew.Services;

namespace Ask.UI.Features.ProtocolNew.Execution;

/// <summary>
/// Координирует неизменную последовательность остановки, сброса, отображения и сохранения результатов выполнения.
/// </summary>
internal sealed class ExecutionFinalizer
{
  /// <summary>
  /// Сервис системного и аппаратного сброса.
  /// </summary>
  private readonly IExecutionSystemResetService _systemResetService;

  /// <summary>
  /// Сервис завершающих операций протокола.
  /// </summary>
  private readonly ProtocolCompletionService _protocolCompletionService;

  /// <summary>
  /// Инициализирует координатор завершения выполнения.
  /// </summary>
  /// <param name="systemResetService">Сервис сброса системы.</param>
  /// <param name="protocolCompletionService">Сервис завершения протокола.</param>
  public ExecutionFinalizer(
    IExecutionSystemResetService systemResetService,
    ProtocolCompletionService protocolCompletionService)
  {
    _systemResetService = systemResetService;
    _protocolCompletionService = protocolCompletionService;
  }

  /// <summary>
  /// Выполняет полную последовательность финализации текущего запуска.
  /// </summary>
  /// <param name="settings">Настройки завершаемого действия.</param>
  /// <param name="protocol">Компонент протокола и управления UI.</param>
  /// <param name="cancelProcessAsync">Операция отмены и ожидания фоновой задачи.</param>
  /// <param name="resetExecutorState">Операция очистки внутреннего состояния исполнителя.</param>
  /// <param name="processingStateChanged">Уведомление об изменении состояния выполнения.</param>
  public async Task FinalizeAsync(
    ActionSettings settings,
    ProtocolUI protocol,
    Func<Task> cancelProcessAsync,
    Action resetExecutorState,
    Action<bool>? processingStateChanged)
  {
    await cancelProcessAsync();
    resetExecutorState();
    await _systemResetService.ResetAsync();

    _protocolCompletionService.PrintIfRequired(settings, protocol);
    SystemStateManager.SetIsLocked(false);

    protocol.ShowOnlyStartButton();
    await _protocolCompletionService.DisplayCompletionAsync(settings, protocol);
    processingStateChanged?.Invoke(false);
    await _protocolCompletionService.SaveAndExposeAsync(settings, protocol);
  }
}
