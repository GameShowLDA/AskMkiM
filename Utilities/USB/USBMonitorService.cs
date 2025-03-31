using System.Management;
using System.Windows.Threading;
using static Utilities.LoggerUtility;

namespace Utilities.USB
{
  /// <summary>
  /// Сервис мониторинга USB-устройств и проверки USB-ключей.
  /// </summary>
  public class USBMonitorService
  {
    private readonly ManagementEventWatcher _insertWatcher;
    private readonly ManagementEventWatcher _removeWatcher;
    private readonly Dispatcher _dispatcher;
    private readonly USBKeyValidator _usbKeyValidator;

    /// <summary>
    /// Событие изменения прав администратора.
    /// </summary>
    public event EventHandler<bool> AdminRightsChanged;

    private bool _adminRights;

    /// <summary>
    /// Текущее состояние прав администратора.
    /// </summary>
    public bool AdminRights
    {
      get => _adminRights;
      set
      {
        if (_adminRights == value)
        {
          return;
        }

        _adminRights = value;
        AdminRightsChanged?.Invoke(this, _adminRights);
      }
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="USBMonitorService"/>.
    /// </summary>
    /// <param name="dispatcher">Диспетчер интерфейса, используемый для выполнения операций в UI-потоке.</param>
    public USBMonitorService(Dispatcher dispatcher)
    {
      _dispatcher = dispatcher;
      _usbKeyValidator = new USBKeyValidator();
      _insertWatcher = CreateWatcher("__InstanceCreationEvent", OnUSBInserted);
      _removeWatcher = CreateWatcher("__InstanceDeletionEvent", OnUSBRemoved);
    }

    /// <summary>
    /// Создаёт наблюдателя за событиями USB.
    /// </summary>
    private ManagementEventWatcher CreateWatcher(string eventType, EventArrivedEventHandler handler)
    {
      var query = new WqlEventQuery($"SELECT * FROM {eventType} WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
      var watcher = new ManagementEventWatcher(query);
      watcher.EventArrived += handler;
      return watcher;
    }

    /// <summary>
    /// Запускает мониторинг USB-устройств.
    /// </summary>
    public void Start()
    {
      _insertWatcher.Start();
      _removeWatcher.Start();
      LogInformation("USB мониторинг запущен.");
      CheckExistingUSBDevices();
    }

    /// <summary>
    /// Останавливает мониторинг USB-устройств.
    /// </summary>
    public void Stop()
    {
      _insertWatcher.Stop();
      _removeWatcher.Stop();
      LogInformation("USB мониторинг остановлен.");
    }

    /// <summary>
    /// Обработчик события подключения USB-устройства.
    /// </summary>
    private void OnUSBInserted(object sender, EventArrivedEventArgs e)
    {
      _dispatcher.Invoke(() =>
      {
        LogInformation("USB устройство подключено.");
        UpdateAdminRights();
      });
    }

    /// <summary>
    /// Обработчик события отключения USB-устройства.
    /// </summary>
    private void OnUSBRemoved(object sender, EventArrivedEventArgs e)
    {
      _dispatcher.Invoke(() =>
      {
        LogInformation("USB устройство отключено.");
        AdminRights = false;
      });
    }

    /// <summary>
    /// Проверяет наличие действительного USB-ключа среди подключенных устройств.
    /// </summary>
    private void CheckExistingUSBDevices()
    {
      _dispatcher.Invoke(UpdateAdminRights);
    }

    /// <summary>
    /// Обновляет статус прав администратора на основе наличия USB-ключа.
    /// </summary>
    private void UpdateAdminRights()
    {
      if (_usbKeyValidator.IsValidUSBKey())
      {
        AdminRights = true;
        LogInformation("Действительный USB ключ обнаружен. Права администратора установлены.");
      }
      else
      {
        LogInformation("Подключенное устройство не является действительным USB ключом.");
      }
    }
  }
}