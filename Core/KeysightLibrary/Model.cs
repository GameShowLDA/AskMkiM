using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;
using Core.Abstract;
using Core.Enum;
using Newtonsoft.Json.Linq;
using static Utilities.LoggerUtility;

namespace Core.KeysightLibrary
{
  /// <summary>
  /// Класс Model предоставляет методы для подключения и взаимодействия с измерительным прибором Agilent 34465A.
  /// </summary>
  public class Model : MeterBase
  {
    #region Поля.

    /// <summary>
    /// Строка подключения к прибору.
    /// </summary>
    private string connectionString;

    // In order to use the following driver class, you need to reference this assembly : [C:\ProgramData\Keysight\Command Expert\ScpiNetDrivers\Ag3466x_2_08.dll]
    /// <summary>
    /// Экземпляр класса Ag3466x для взаимодействия с прибором.
    /// </summary>
    static private Ag3466x v34465A;

    /// <summary>
    /// Флаг, указывающий на состояние подключения.
    /// </summary>
    private bool isConnected;

    #endregion

    /// <summary>
    /// Асинхронно создает и инициализирует экземпляр класса Model.
    /// </summary>
    /// <returns>Экземпляр класса Model.</returns>
    public static async Task<Model> CreateAsync()
    {
      var model = new Model();
      await model.InitializeAsync();
      LogInformation("Экземпляр Model создан и инициализирован.");
      return model;
    }

    /// <summary>
    /// Асинхронно инициализирует экземпляр класса Model.
    /// </summary>
    private async Task InitializeAsync()
    {
      LogInformation("Начало инициализации экземпляра Model.");
      isConnected = await ConnectAsync();
      LogInformation($"Инициализация Model завершена. Подключение: {(isConnected ? "успешно" : "не удалось")}");
    }

    /// <summary>
    /// Асинхронно подключается к измерительному прибору.
    /// </summary>
    /// <returns>Возвращает true, если подключение успешно, иначе false.</returns>
    public async override Task<bool> ConnectAsync()
    {
      LogInformation("Начало процесса подключения.");
      var localIPs = GetLocalIPAddresses();
      var tasks = localIPs.Select(ip => ScanAndConnectAsync(ip)).ToList();
      var results = await Task.WhenAll(tasks);
      isConnected = results.Any(result => result);
      LogInformation($"Процесс подключения завершен. Результат: {(isConnected ? "успешно" : "не удалось")}");
      return isConnected;
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

          connectionString = $"TCPIP0::{address}::inst0::INSTR";
          v34465A = new Ag3466x(connectionString);
          var connectInstrumentTask = Task.Run(() => v34465A.Connect());
          if (await Task.WhenAny(connectInstrumentTask, Task.Delay(1000)) != connectInstrumentTask)
          {
            LogWarning($"Таймаут подключения к инструменту для адреса: {address}");
            return false;
          }

          await connectInstrumentTask;

          if (CheckConnection())
          {
            IPAddress = IPAddress.Parse(address);
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

    /// <summary>
    /// Проверяет подключение к прибору.
    /// </summary>
    /// <returns>Возвращает true, если подключение успешно, иначе false.</returns>
    public override bool CheckConnection()
    {
      LogInformation("Проверка подключения к инструменту.");
      try
      {
        string idn;
        v34465A.SCPI.IDN.Query(out idn);
        LogInformation($"Получен IDN: {idn}");
        return !string.IsNullOrEmpty(idn);
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при проверке подключения: {ex.Message}");
        return false;
      }
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
    /// Отключает прибор.
    /// </summary>
    public override void Disconnect()
    {
      LogInformation("Отключение от инструмента.");
      v34465A?.Disconnect();
      LogInformation("Отключение от инструмента выполнено.");
    }

    /// <summary>
    /// Настраивает мультиметр на измерение сопротивления.
    /// </summary>
    public override void SetResistanceMode()
    {
      try
      {
        LogInformation("Попытка установить мультиметр в режим измерения сопротивления.");
        ResistanceMeasurement.SetResistanceMode(v34465A);

      }
      catch (Exception)
      {
        LogError("Ошибка установки мультиметра в режим измерения сопротивления.");
      }
    }


    /// <summary>
    /// Настраивает мультиметр на измерение ёмкости.
    /// </summary>
    public override void SetCapacitanceMode()
    {
      try
      {
        LogInformation("Попытка установить мультиметр в режим измерения ёмкости.");
        CapacitanceMeasurement.SetCapacitanceMode(v34465A);

      }
      catch (Exception)
      {
        LogError("Ошибка установки мультиметра в режим измерения ёмкости.");
      }
    }

    /// <summary>
    /// Реализация метода измерения напряжения постоянного тока.
    /// </summary>
    /// <returns>Измеренное напряжение.</returns>
    public override double MeasureVoltageDC()
    {
      LogInformation("Измерение напряжения постоянного тока.");
      double result = VoltageMeasurement.MeasureVoltageDC(v34465A);
      LogInformation($"Результат измерения напряжения постоянного тока: {result}");
      return result;
    }

    /// <summary>
    /// Измеряет непрерывность цепи.
    /// </summary>
    /// <returns>Результат измерения непрерывности.</returns>
    public override double MeasureContinuity()
    {
      LogInformation("Измерение непрерывности.");
      double result = ContinuityMeasurement.MeasureContinuity(v34465A);
      LogInformation($"Результат измерения непрерывности: {result}");
      return result;
    }

    /// <summary>
    /// Измеряет сопротивление.
    /// </summary>
    /// <returns>Результат измерения сопротивления.</returns>
    public override double MeasureResistance()
    {
      LogInformation("Измерение сопротивления.");
      double result = ResistanceMeasurement.MeasureResistance(v34465A);
      LogInformation($"Результат измерения сопротивления: {result}");
      return result;
    }


    /// <summary>
    /// Измеряет ёмкость.
    /// </summary>
    /// <returns>Результат измерения ёмкости.</returns>
    public override double MeasureCapacitance()
    {
      LogInformation("Измерение ёмкости.");
      double result = CapacitanceMeasurement.MeasureCapacitance(v34465A);
      LogInformation($"Результат измерения ёмкости: {result}");
      return result;
    }

    /// <summary>
    /// Очищаем буфер.
    /// </summary>
    public override void ClearBuffer()
    {
      v34465A.SCPI.CLS.Command();
    }

    /// <summary>
    /// Самоконтроль мультиметра.
    /// </summary>
    public void SelfTest()
    {
      int test = 0;
      v34465A.SCPI.TEST.ALL.Query(out test);
      Console.WriteLine($"Код самоконтроля: {test}");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    public Model()
    {
      this.Name = "KEYSIGHT 34465A";
      this.Description = "6,5-разрядный цифровой мультиметр KEYSIGHT 34465A с запатентованной технологией Truevolt для большей точности и более быстрого получения результатов измерений.";
      this.DeviceType = Enum.DeviceEnum.Type.FastMeter;
    }

    /// <summary>
    /// Создает экземпляр Model из объекта.
    /// </summary>
    /// <param name="obj">Объект для преобразования (может быть JSON строкой или JObject).</param>
    /// <returns>Новый экземпляр Model или null, если входной объект null.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если объект не является JSON строкой или JObject.</exception>
    public static Model CreateFromObject(object obj)
    {
      // Проверка на null
      if (obj == null)
      {
        return null;
      }

      // Если объект уже является Model, возвращаем его
      if (obj is Model model)
      {
        return model;
      }

      // Получаем JObject из входного объекта
      JObject jObject;
      if (obj is string jsonString)
      {
        jObject = JObject.Parse(jsonString);
      }
      else if (obj is JObject jObj)
      {
        jObject = jObj;
      }
      else
      {
        throw new ArgumentException("Объект должен быть JSON строкой или JObject", nameof(obj));
      }

      try
      {
        // Создаем новый экземпляр Model с данными из JObject
        var newModel = new Model()
        {
          Name = jObject["Name"]?.Value<string>(),
          Description = jObject["Description"]?.Value<string>(),
          DeviceType = (DeviceEnum.Type)jObject["DeviceType"].Value<int>(),
        };

        return newModel;
      }
      catch (Exception ex)
      {
        throw new ArgumentException($"Ошибка при создании Model из объекта: {ex.Message}", nameof(obj), ex);
      }
    }
  }
}
