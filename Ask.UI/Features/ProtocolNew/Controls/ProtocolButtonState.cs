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
  /// Основное действие завершено и доступны повтор, цикл и завершение.
  /// </summary>
  AdditionalActions,

  /// <summary>
  /// Доступно только завершение текущего пользовательского сценария.
  /// </summary>
  ExitOnly,
}
