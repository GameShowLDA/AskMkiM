using System.IO.Ports;
using System.Management;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace NewCore.Base.Device
{
  /// <summary>
  /// Абстрактный класс <see cref="DeviceWithCOM"/> представляет базовый функционал устройства, 
  /// подключаемого через COM-порт.
  /// </summary>
  /// <remarks>
  /// Этот класс реализует интерфейс <see cref="IDevice"/> и предоставляет базовые методы для подключения 
  /// и отключения устройств через последовательный порт (COM).
  /// </remarks>
  public abstract class DeviceWithCOM : IDevice
  {
    /// <summary>
    /// Получает или задает имя устройства.
    /// </summary>
    /// <value>Имя устройства в виде строки.</value>
    public string Name { get; set; }

    /// <summary>
    /// Получает или задает описание устройства.
    /// </summary>
    /// <value>Описание устройства в виде строки.</value>
    public string Description { get; set; }

    /// <summary>
    /// Получает или задает COM-порт, используемый для подключения устройства.
    /// </summary>
    /// <value>Экземпляр класса <see cref="SerialPort"/>, представляющий COM-порт.</value>
    public SerialPort COMPort { get; set; }

    /// <summary>
    /// Получает или задает идентификатор производителя устройства (Vendor ID).
    /// </summary>
    /// <value>VID устройства в виде строки.</value>
    public string VID { get; set; }

    /// <summary>
    /// Получает или задает идентификатор продукта устройства (Product ID).
    /// </summary>
    /// <value>PID устройства в виде строки.</value>
    public string PID { get; set; }

    /// <summary>
    /// Получает или задает номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Получает или задает детали подключения устройства.
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Получает или задает тип устройства.
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Получает или задает флаг, указывающий, является ли устройство подключаемым.
    /// </summary>
    public bool IsAttachableDevice { get; set; }

    /// <summary>
    /// Получает или задает идентификатор устройства.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Получает или задает класс устройства.
    /// </summary>
    public string DeviceClass { get; set; }

    /// <summary>
    /// Получает или задает скорость передачи данных (Baud Rate) для COM-порта.
    /// </summary>
    /// <value>Скорость передачи данных в бит/с. Обычно по умолчанию 9600.</value>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// Получает или задает количество стоповых бит для COM-порта.
    /// </summary>
    /// <value>Стоповые биты, тип <see cref="StopBits"/>. Обычно по умолчанию <see cref="StopBits.One"/>.</value>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// Получает или задает количество бит данных для COM-порта.
    /// </summary>
    /// <value>Количество бит данных. Обычно по умолчанию 8.</value>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// Получает или задает режим чётности для COM-порта.
    /// </summary>
    /// <value>Чётность, тип <see cref="Parity"/>. Обычно по умолчанию <see cref="Parity.None"/>.</value>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// Получает или задает режим управления потоком для COM-порта.
    /// </summary>
    /// <value>Режим управления потоком в виде строки (например, "Xon/Xoff", "Аппаратное", "Нет").</value>
    public string FlowControl { get; set; } = "Нет";

    /// <summary>
    /// Подключается к устройству через COM-порт.
    /// </summary>
    /// <remarks>
    /// Метод проверяет, открыт ли порт, и открывает его при необходимости. 
    /// Также настраивает параметры COM-порта, если они не были заданы ранее.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Выбрасывается, если COM-порт не инициализирован.</exception>
    /// <exception cref="UnauthorizedAccessException">Выбрасывается, если доступ к порту запрещен.</exception>
    public virtual void Connect()
    {
      if (COMPort == null)
      {
        throw new InvalidOperationException("COM-порт не инициализирован.");
      }

      try
      {
        if (COMPort.BaudRate == 0)
        {
          COMPort.BaudRate = 9600; // Скорость передачи данных
        }

        if (COMPort.Parity == Parity.None)
        {
          COMPort.Parity = Parity.None; // Четность
        }

        if (COMPort.DataBits == 0)
        {
          COMPort.DataBits = 8; // Количество бит данных
        }

        if (COMPort.StopBits == StopBits.None)
        {
          COMPort.StopBits = StopBits.One; // Стоповые биты
        }

        if (!COMPort.IsOpen)
        {
          COMPort.Open();
        }

        Console.WriteLine($"Успешно подключено к устройству {Name} через COM-порт {COMPort.PortName}");
      }
      catch (UnauthorizedAccessException ex)
      {
        Console.WriteLine($"Ошибка доступа к COM-порту {COMPort.PortName}: {ex.Message}");
        throw;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при подключении к устройству {Name}: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Отключается от устройства через COM-порт.
    /// </summary>
    /// <remarks>
    /// Метод проверяет, открыт ли порт, и закрывает его при необходимости.
    /// </remarks>
    public virtual void Disconnect()
    {
      if (COMPort == null)
      {
        Console.WriteLine("COM-порт не инициализирован.");
        return;
      }

      try
      {
        if (COMPort.IsOpen)
        {
          COMPort.Close();
        }

        Console.WriteLine($"Успешно отключено от устройства {Name} через COM-порт {COMPort.PortName}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при отключении от устройства {Name}: {ex.Message}");
      }
    }

    /// <summary>
    /// Выполняет поиск COM-порта устройства на основе указанных идентификаторов производителя (VID) и продукта (PID).
    /// </summary>
    /// <param name="vid">Идентификатор производителя устройства (VID) в формате строки.</param>
    /// <param name="pid">Идентификатор продукта устройства (PID) в формате строки.</param>
    /// <returns>Строка с именем COM-порта (например, "COM3") или null, если устройство не найдено.</returns>
    public virtual string FindComPort(string vid, string pid)
    {
      try
      {
        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB\\\\VID_" + vid + "&PID_" + pid + "%'"))
        {
          foreach (ManagementObject device in searcher.Get())
          {
            string caption = device["Caption"]?.ToString() ?? "";
            if (caption.Contains("COM"))
            {
              int start = caption.IndexOf("COM");
              int end = caption.IndexOf(")", start);
              if (start >= 0 && end >= 0)
              {
                return caption.Substring(start, end - start);
              }
            }
          }
        }
      }
      catch (Exception)
      {
        return null;
      }

      return null;
    }

    /// <summary>
    /// Проверяет соединение с устройством.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="bool"/>, указывающий на наличие соединения.
    /// </returns>
    public virtual async Task<bool> Initialize()
    {
      try
      {
        LogInformation("Открываем порт...");
        if (COMPort != null)
        {
          using (var port = COMPort)
          {
            await Task.Run(() => port.Open());
            LogInformation("Порт открыт");

            string query = $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%({COMPort})%'";
            using (var searcher = new ManagementObjectSearcher(query))
            {
              var results = await Task.Run(() => searcher.Get().Cast<ManagementObject>().ToList());

              foreach (var obj in results)
              {
                string deviceID = obj["DeviceID"]?.ToString() ?? string.Empty;
                if (deviceID.Contains(VID) && deviceID.Contains(PID))
                {
                  LogInformation($"Устройство найдено по VID/PID: {VID}, {PID}");
                  return true;
                }
              }
            }

            LogError("Устройство не найдено по VID/PID");
            return false;
          }
        }

        return false;
      }
      catch (UnauthorizedAccessException ex)
      {
        LogError($"Ошибка доступа к порту: {ex.Message}");
        return false;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при проверке соединения: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Чтение порта.
    /// </summary>
    /// <returns>Ответ с устройства.</returns>
    public virtual async Task<string> ReadLineAsync()
    {
      try
      {
        if (!COMPort.IsOpen)
        {
          COMPort.Open();
        }

        if (COMPort.BytesToRead > 0)
        {
          return await Task.Run(() => COMPort.ReadLine());
        }
        else
        {
          LogError("Нет данных для чтения.");
          return string.Empty;
        }
      }
      catch (TimeoutException ex)
      {
        LogError($"Таймаут при чтении из порта: {ex.Message}");
        return string.Empty;
      }
      catch (IOException ex)
      {
        LogError($"Ошибка ввода-вывода при чтении из порта: {ex.Message}");
        return string.Empty;
      }
      catch (UnauthorizedAccessException ex)
      {
        LogError($"Доступ к порту запрещен: {ex.Message}");
        return string.Empty;
      }
      catch (InvalidOperationException ex)
      {
        LogError($"Операция с портом не может быть выполнена: {ex.Message}");
        return string.Empty;
      }
      catch (Exception ex)
      {
        LogError($"Неизвестная ошибка при чтении из порта: {ex.Message}");
        return string.Empty;
      }
    }

    /// <summary>
    /// Отправка на сообщения в порт.
    /// </summary>
    /// <param name="command">Отправляемое сообщение.</param>
    public virtual async Task WriteLineAsync(string command)
    {
      try
      {
        if (!COMPort.IsOpen)
        {
          COMPort.Open();
        }

        COMPort.WriteLine(command);
        await Task.Delay(100); // Задержка для обработки команды
      }
      catch (UnauthorizedAccessException ex)
      {
        LogError($"Ошибка доступа к порту: {ex.Message}");
      }
      catch (IOException ex)
      {
        LogError($"Ошибка ввода-вывода при работе с портом: {ex.Message}");
      }
      catch (Exception ex)
      {
        LogError($"Неизвестная ошибка при отправке команды: {ex.Message}");
      }
    }
  }
}
