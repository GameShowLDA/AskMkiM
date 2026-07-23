namespace Ask.UI.Features.ProtocolNew.Controls;

/// <summary>
/// Определяет базовые состояния панели управления выполнением.
/// </summary>
internal enum ProtocolButtonState
{
  /// <summary>
  /// Все управляющие кнопки скрыты.
  /// </summary>
  Hidden,

  /// <summary>
  /// Выполнение не запущено и доступна только кнопка запуска.
  /// </summary>
  Ready,

  /// <summary>
  /// Выполнение активно и доступны пауза и завершение.
  /// </summary>
  Running,

  /// <summary>
  /// Выполнение приостановлено и доступны продолжение и завершение.
  /// </summary>
  Paused,

  /// <summary>
  /// Доступны повтор и завершение после аппаратной ошибки.
  /// </summary>
  AdditionalActions,

  /// <summary>
  /// Доступны повтор, продолжение и завершение интерактивной операции.
  /// </summary>
  InteractiveActions,

  /// <summary>
  /// Доступны только повтор и продолжение интерактивной операции.
  /// </summary>
  RetryOrContinue,

  /// <summary>
  /// Доступно только завершение текущего пользовательского сценария.
  /// </summary>
  ExitOnly,
}
