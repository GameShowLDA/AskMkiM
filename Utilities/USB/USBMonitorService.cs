using System.Management;
using System.Windows.Threading;
using static Utilities.LoggerUtility;

namespace Utilities.USB
{
  public class USBMonitorService
  {
    private ManagementEventWatcher insertWatcher;
    private ManagementEventWatcher removeWatcher;
    private Dispatcher dispatcher;
    private USBKeyValidator usbKeyValidator;

    // Добавляем событие
    public event EventHandler<bool> AdminRightsChanged;

    // Свойство для отслеживания прав администратора
    private bool adminRights;
    public bool AdminRights
    {
      get { return adminRights; }
      private set
      {
        if (adminRights != value)
        {
          adminRights = value;
          OnAdminRightsChanged(adminRights); // Срабатывает событие при изменении
        }
      }
    }

    public USBMonitorService(Dispatcher dispatcher)
    {
      this.dispatcher = dispatcher;
      this.usbKeyValidator = new USBKeyValidator();
      InitializeWatchers();
    }

    private void InitializeWatchers()
    {
      WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
      insertWatcher = new ManagementEventWatcher(insertQuery);
      insertWatcher.EventArrived += (s, e) => OnUSBInserted();

      WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
      removeWatcher = new ManagementEventWatcher(removeQuery);
      removeWatcher.EventArrived += (s, e) => OnUSBRemoved();
    }

    public void Start()
    {
      insertWatcher.Start();
      removeWatcher.Start();
      LogInformation("USB мониторинг запущен.");
      CheckExistingUSBDevices();
    }

    public void Stop()
    {
      insertWatcher.Stop();
      removeWatcher.Stop();
      LogInformation("USB мониторинг остановлен.");
    }

    private void OnUSBInserted()
    {
      dispatcher.Invoke(async () =>
      {
        LogInformation("USB устройство подключено.");
        if (usbKeyValidator.IsValidUSBKey())
        {
          AdminRights = true; // Устанавливаем права
          LogInformation("Действительный USB ключ обнаружен. Права администратора установлены.");
        }
        else
        {
          LogInformation("Подключенное устройство не является действительным USB ключом.");
        }
      });
    }

    private void OnUSBRemoved()
    {
      dispatcher.Invoke(async () =>
      {
        LogInformation("USB устройство отключено.");
        AdminRights = false; // Сбрасываем права
      });
    }

    private void CheckExistingUSBDevices()
    {
      dispatcher.Invoke(async () =>
      {
        LogInformation("Проверка уже подключенных USB устройств.");
        if (usbKeyValidator.IsValidUSBKey())
        {
          AdminRights = true; // Устанавливаем права
          LogInformation("Действительный USB ключ обнаружен среди уже подключенных устройств. Права администратора установлены.");
        }
        else
        {
          LogInformation("Среди уже подключенных устройств нет действительных USB ключей.");
        }
      });
    }

    protected virtual void OnAdminRightsChanged(bool newRights)
    {
      AdminRightsChanged?.Invoke(this, newRights); // Генерация события
    }
  }
}
