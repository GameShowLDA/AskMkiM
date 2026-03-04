namespace Ask.Core.Shared.Metadata.View
{
  public interface IUsbMonitorView
  {
    /// <summary>
    /// Включает или отключает мониторинг USB в зависимости от режима администратора.
    /// </summary>
    /// <param name="admin">Если <c>true</c> — включить режим администратора, иначе — отключить.</param>
    void SetUsbMonitoring(bool admin);

    /// <summary>
    /// Останавливает сервис мониторинга USB.
    /// </summary>
    void StopUsbMonitoring();

    /// <summary>
    /// Текущее состояние прав администратора.
    /// </summary>
    bool AdminRights { get; set; }

    /// <summary>
    /// Событие изменения прав администратора.
    /// </summary>
    event EventHandler<bool> AdminRightsChanged;
  }
}
