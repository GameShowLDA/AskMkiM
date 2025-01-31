using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;
using NewCore.Base;
using NewCore.Function.KeysightLibrary;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static Utilities.LoggerUtility;

namespace NewCore.Device
{
  public class Keysight3466 : DeviceWithIP
  {
    public Keysight3466(IPAddress ip) : base(ip) 
    {
      Name = "Keysight 3466";
      Description = "Реализовать описание в NewCore.Device.Keysight3466";
      ConnectAsync().ConfigureAwait(true);
    }

    private string ConnectionString;

    private Ag3466x ag3466X;
    public CapacitanceMeasurement CapacitanceMeasurement => new(ag3466X);
    public ContinuityMeasurement ContinuityMeasurement => new(ag3466X);
    public CurrentMeasurement CurrentMeasurement => new(ag3466X);
    public ResistanceMeasurement ResistanceMeasurement => new(ag3466X);
    public VoltageMeasurement VoltageMeasurement => new(ag3466X);

    public override Task<bool> IsConnectedAsync()
    {
      Task.Run(() =>
      {
        LogInformation("Проверка подключения к инструменту.");
        try
        {
          string idn;
          ag3466X.SCPI.IDN.Query(out idn);
          LogInformation($"Получен IDN: {idn}");
          return !string.IsNullOrEmpty(idn);
        }
        catch (Exception ex)
        {
          LogError($"Ошибка при проверке подключения: {ex.Message}");
          return false;
        }
      });

      return Task.FromResult(true);
    }

    /// <summary>
    /// Асинхронно подключается к измерительному прибору.
    /// </summary>
    /// <returns>Возвращает true, если подключение успешно, иначе false.</returns>
    public async Task<Keysight3466> ConnectAsync()
    {
      try
      {
        LogInformation("Начало процесса подключения.");
        var localIPs = GetLocalIPAddresses();
        var tasks = localIPs.Select(ip => ScanAndConnectAsync(ip)).ToList();
        var results = await Task.WhenAll(tasks);
        bool isConnected = results.Any(result => result);
        LogInformation($"Процесс подключения завершен. Результат: {(isConnected ? "успешно" : "не удалось")}");
        if (isConnected)
        {
          return this;
        }
      }
      catch (Exception ex)
      {
        LogError($"Процесс подключения завершен с ошибками : {ex}");
      }
      return null;
    }

    /// <summary>
    /// Получает список локальных IP-адресов.
    /// </summary>
    /// <returns>Список локальных IP-адресов.</returns>
    private List<string> GetLocalIPAddresses()
    {
      LogInformation("Получение локальных IP-адресов.");
      var localIPs = new List<string>();
      foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
      {
        if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        {
          foreach (var ip in ni.GetIPProperties().UnicastAddresses)
          {
            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
            {
              localIPs.Add(ip.Address.ToString());
            }
          }
        }
      }

      LogInformation($"Найдено {localIPs.Count} локальных IP-адресов.");
      return localIPs;
    }

    /// <summary>
    /// Асинхронно сканирует и подключается к прибору по указанному IP-адресу.
    /// </summary>
    /// <param name="baseIP">Базовый IP-адрес для сканирования.</param>
    /// <returns>Возвращает true, если подключение успешно, иначе false.</returns>
    private async Task<bool> ScanAndConnectAsync(string baseIP)
    {
      LogInformation($"Начало сканирования и подключения к базовому IP: {baseIP}");
      var tasks = new List<Task<bool>>();
      for (int i = 1; i <= 254; i++)
      {
        string ip = baseIP.Substring(0, baseIP.LastIndexOf(".") + 1) + i;
        tasks.Add(TryConnectAsync(ip));
      }

      while (tasks.Count > 0)
      {
        var completedTask = await Task.WhenAny(tasks);
        tasks.Remove(completedTask);
        if (await completedTask)
        {
          LogInformation($"Успешное подключение к IP в диапазоне: {baseIP}");
          return true;
        }
      }

      LogWarning($"Не удалось подключиться ни к одному IP в диапазоне: {baseIP}");
      return false;
    }

    /// <summary>
    /// Асинхронно пытается подключиться к прибору по указанному IP-адресу.
    /// </summary>
    /// <param name="address">IP-адрес для подключения.</param>
    /// <returns>Возвращает true, если подключение успешно, иначе false.</returns>
    public async Task<bool> TryConnectAsync(string address)
    {
      LogInformation($"Попытка подключения к адресу: {address}");
      try
      {
        using (var client = new TcpClient())
        {
          var connectTask = client.ConnectAsync(address, 5025);
          if (await Task.WhenAny(connectTask, Task.Delay(500)) != connectTask)
          {
            LogWarning($"Таймаут подключения для адреса: {address}");
            return false;
          }

          await connectTask;

          ConnectionString = $"TCPIP0::{address}::inst0::INSTR";
          ag3466X = new Ag3466x(ConnectionString);
          var connectInstrumentTask = Task.Run(() => ag3466X.Connect());
          if (await Task.WhenAny(connectInstrumentTask, Task.Delay(1000)) != connectInstrumentTask)
          {
            LogWarning($"Таймаут подключения к инструменту для адреса: {address}");
            return false;
          }

          await connectInstrumentTask;

          if (await IsConnectedAsync())
          {
            this.IPAddress = System.Net.IPAddress.Parse(address);
            LogInformation($"Успешное подключение к {IPAddress}");
            return true;
          }
        }
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при подключении к {address}: {ex.Message}");
      }

      LogWarning($"Не удалось подключиться к {address}");
      return false;
    }
  }
}
