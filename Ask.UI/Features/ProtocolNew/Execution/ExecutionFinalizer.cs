using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.UI.Controls.ProtocolNew;
using Ask.UI.Features.ProtocolNew.Protocol;
using Ask.UI.Features.ProtocolNew.Services;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.ProtocolNew.Execution;

/// <summary>
/// Координирует неизменную последовательность остановки, сброса, отображения и сохранения результатов выполнения.
/// </summary>
internal sealed class ExecutionFinalizer
{
  /// <summary>
  /// Сервис сброса глобального состояния выполнения.
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
  /// <param name="resetUsedEquipmentAsync">Операция сброса использованного оборудования.</param>
  /// <param name="resetExecutorState">Операция очистки внутреннего состояния исполнителя.</param>
  /// <param name="processingStateChanged">Уведомление об изменении состояния выполнения.</param>
  public async Task FinalizeAsync(
    ActionSettings settings,
    ProtocolUI protocol,
    Func<Task> cancelProcessAsync,
    Func<Task> resetUsedEquipmentAsync,
    Action resetExecutorState,
    Action<bool>? processingStateChanged)
  {
    await RunMandatoryStepsAsync(
      ("остановка выполняемой задачи", cancelProcessAsync),
      ("финальный сброс использованного оборудования", resetUsedEquipmentAsync),
      ("сброс состояния исполнителя", AsAsync(resetExecutorState)),
      ("сброс глобального состояния выполнения", _systemResetService.ResetAsync),
      ("печать протокола", AsAsync(
        () => _protocolCompletionService.PrintIfRequired(settings, protocol))),
      ("снятие блокировки системы", AsAsync(() => SystemStateManager.SetIsLocked(false))),
      ("восстановление кнопки запуска", AsAsync(protocol.ShowOnlyStartButton)),
      ("отображение результата выполнения",
        () => _protocolCompletionService.DisplayCompletionAsync(settings, protocol)),
      ("уведомление о завершении выполнения",
        AsAsync(() => processingStateChanged?.Invoke(false))),
      ("сохранение протоколов",
        () => _protocolCompletionService.SaveAndExposeAsync(settings, protocol)));
  }

  internal static async Task RunMandatoryStepsAsync(
    params (string Name, Func<Task> Operation)[] steps)
  {
    using var finalizationScope = EquipmentExecutionContext.EnterMandatoryFinalization();

    foreach (var step in steps)
    {
      try
      {
        await step.Operation();
      }
      catch (Exception ex)
      {
        LogException($"Ошибка обязательного завершающего действия: {step.Name}.", ex);
      }
    }
  }

  private static Func<Task> AsAsync(Action operation)
  {
    return () =>
    {
      operation();
      return Task.CompletedTask;
    };
  }
}
