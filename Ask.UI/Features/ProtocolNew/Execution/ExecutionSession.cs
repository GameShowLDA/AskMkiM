using Ask.Core.Services.Devices;
using Ask.Core.Shared.DTO.Executor;

namespace Ask.UI.Features.ProtocolNew.Execution;

/// <summary>
/// Хранит ресурсы и состояние, относящиеся строго к одному запуску исполнительного процесса.
/// </summary>
internal sealed class ExecutionSession : IDisposable
{
  /// <summary>
  /// Инициализирует состояние нового запуска.
  /// </summary>
  /// <param name="settings">Настройки выполняемого действия.</param>
  public ExecutionSession(ActionSettings settings)
  {
    Settings = settings;
    Cancellation = new CancellationTokenSource();
    EquipmentUsage = EquipmentUsageTracker.BeginSession();
  }

  /// <summary>
  /// Настройки текущего действия.
  /// </summary>
  public ActionSettings Settings { get; }

  /// <summary>
  /// Источник отмены текущего запуска.
  /// </summary>
  public CancellationTokenSource Cancellation { get; }

  /// <summary>
  /// Оборудование, использованное текущим запуском.
  /// </summary>
  public EquipmentUsageSession EquipmentUsage { get; }

  /// <summary>
  /// Фоновая задача, выполняющая делегат режима.
  /// </summary>
  public Task? ProcessTask { get; set; }

  /// <summary>
  /// Освобождает ресурсы текущего запуска.
  /// </summary>
  public void Dispose()
  {
    EquipmentUsage.Dispose();
    Cancellation.Dispose();
  }
}
