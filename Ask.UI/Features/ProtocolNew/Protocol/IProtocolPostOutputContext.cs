namespace Ask.UI.Features.ProtocolNew.Protocol
{
  /// <summary>
  /// Предоставляет операции исполнителя и кнопок, необходимые после вывода записи протокола.
  /// </summary>
  internal interface IProtocolPostOutputContext
  {
    /// <summary>Возвращает признак установленной паузы выполнения.</summary>
    bool IsPaused { get; }

    /// <summary>Возвращает актуальный токен отмены текущего выполнения.</summary>
    CancellationToken GetCancellationToken();

    /// <summary>Ожидает снятия уже установленной паузы.</summary>
    /// <param name="cancellationToken">Токен отмены выполнения.</param>
    Task WaitWhilePausedAsync(CancellationToken cancellationToken);

    /// <summary>Устанавливает выполнение на паузу.</summary>
    Task PauseAsync();

    /// <summary>Отображает кнопки состояния паузы.</summary>
    void ShowPauseButtons();

    /// <summary>Отображает кнопки активного выполнения.</summary>
    /// <param name="showStepButtons">Признак отображения пошаговых кнопок.</param>
    void ShowRunningButtons(bool showStepButtons);
  }
}
