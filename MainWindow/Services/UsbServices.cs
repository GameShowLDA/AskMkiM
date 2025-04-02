using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Utilities.USB;

namespace MainWindowProgram.Services
{
  public class UsbServices
  {
    public readonly USBMonitorService UsbMonitorService = new USBMonitorService(Application.Current.Dispatcher);

    /// <summary>
    /// Включает или отключает мониторинг USB в зависимости от режима администратора.
    /// </summary>
    /// <param name="admin">Если <c>true</c> — включить режим администратора, иначе — отключить.</param>
    public void SetUsbMonitoring(bool admin)
    {
      if (!admin)
      {
        UsbMonitorService.Start();
      }
      else
      {
        UsbMonitorService.AdminRights = admin;
      }
    }

    /// <summary>
    /// Останавливает сервис мониторинга USB.
    /// </summary>
    public void StopUsbMonitoring()
    {
      UsbMonitorService.Stop();
    }
  }
}
