using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public void StopUsbMonitoring();

    /// <summary>
    /// Текущее состояние прав администратора.
    /// </summary>
    public bool AdminRights { get; set; }

    /// <summary>
    /// Событие изменения прав администратора.
    /// </summary>
    public event EventHandler<bool> AdminRightsChanged;
  }
}
