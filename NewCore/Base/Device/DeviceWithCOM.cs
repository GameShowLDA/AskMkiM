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
    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public string ConnectionDetails { get; set; }

    /// <inheritdoc />
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Получает или задает флаг, указывающий, является ли устройство подключаемым.
    /// </summary>
    public bool IsAttachableDevice { get; set; }

    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public IConnectable ConnectableManager { get; set; }

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
