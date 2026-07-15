namespace Ask.UI.Features.ProtocolNew.Execution;

/// <summary>
/// Управляет эксклюзивным доступом к общему контуру выполнения и не допускает
/// одновременный запуск нескольких исполнительных процессов.
/// </summary>
internal interface IExecutionRunGuard
{
  /// <summary>
  /// Пытается закрепить общий контур выполнения за указанным владельцем.
  /// </summary>
  /// <param name="processName">Отображаемое имя запускаемого процесса.</param>
  /// <param name="owner">Объект, которому должен принадлежать захваченный слот.</param>
  /// <param name="activeProcessName">Имя уже выполняемого процесса, если слот занят.</param>
  /// <returns><see langword="true"/>, если слот успешно захвачен.</returns>
  bool TryAcquire(string processName, object owner, out string activeProcessName);

  /// <summary>
  /// Освобождает слот, если он принадлежит указанному владельцу.
  /// </summary>
  /// <param name="owner">Текущий владелец слота.</param>
  void Release(object owner);
}
