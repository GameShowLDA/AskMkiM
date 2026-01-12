using Ask.Core.Services.Usb;
using System.Windows;

namespace MainWindowProgram.Services
{
  public class UsbServices
  {
    public readonly UsbMonitorService UsbMonitorService = new UsbMonitorService(Application.Current.Dispatcher);

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
