
using System.Management;

namespace Ask.Core.Services.Usb
{
  /// <summary>
  /// Отслеживает события подключения и отключения USB-устройств.
  /// </summary>
  internal class UsbDeviceWatcher
  {
    /// <summary>
    /// Событие, возникающее при подключении USB-устройства.
    /// </summary>
    public event EventHandler USBDeviceInserted;

    /// <summary>
    /// Событие, возникающее при отключении USB-устройства.
    /// </summary>
    public event EventHandler USBDeviceRemoved;

    private ManagementEventWatcher insertWatcher;
    private ManagementEventWatcher removeWatcher;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="USBDeviceWatcher"/>.
    /// </summary>
    public UsbDeviceWatcher()
    {
      WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
      insertWatcher = new ManagementEventWatcher(insertQuery);
      insertWatcher.EventArrived += (s, e) => USBDeviceInserted?.Invoke(this, EventArgs.Empty);

      WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
      removeWatcher = new ManagementEventWatcher(removeQuery);
      removeWatcher.EventArrived += (s, e) => USBDeviceRemoved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Запускает отслеживание событий USB-устройств.
    /// </summary>
    public void Start()
    {
      insertWatcher.Start();
      removeWatcher.Start();
    }

    /// <summary>
    /// Останавливает отслеживание событий USB-устройств.
    /// </summary>
    public void Stop()
    {
      insertWatcher.Stop();
      removeWatcher.Stop();
    }
  }
}
