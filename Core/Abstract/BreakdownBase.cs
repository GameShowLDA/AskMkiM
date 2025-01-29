using System.IO.Ports;
using System.Management;
using Core.Enum;
using Core.Model;

namespace Core.Abstract
{
  /// <summary>
  /// Класс, представляющий стандартные методы пробойных установок.
  /// </summary>
  public abstract class BreakdownBase : DeviceModel
  {
    /// <summary>
    /// COM порт для управления пробойной установкой.
    /// </summary>
    public SerialPort Port { get; set; }

    /// <summary>
    /// Устанавливает соединение с устройством.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="Task{bool}"/>, представляющий асинхронную операцию. 
    /// Результат указывает, было ли успешно установлено соединение.
    /// </returns>
    public abstract bool Connect();

    /// <summary>
    /// Проверяет соединение с устройством.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="bool"/>, указывающий на наличие соединения.
    /// </returns>
    public abstract bool CheckConnection();

    /// <summary>
    /// Разрывает соединение с устройством.
    /// </summary>
    public abstract void Disconnect();

    /// <summary>
    /// Чтение порта.
    /// </summary>
    /// <returns>Ответ с устройства</returns>
    public abstract Task<string> ReadLineAsync();


    /// <summary>
    /// Отправка на сообщения в порт.
    /// </summary>
    /// <param name="command">Отправляемое сообщение.</param>
    public abstract Task WriteLineAsync(string command);

    /// <summary>
    /// Статический фабричный метод для создания экземпляра BreakdownBase или его производных классов.
    /// </summary>
    /// <typeparam name="T">Тип создаваемого устройства, должен быть производным от BreakdownBase.</typeparam>
    /// <param name="type">Тип устройства.</param>
    /// <param name="name">Имя устройства.</param>
    /// <param name="description">Описание устройства.</param>
    /// <param name="number">Номер устройства.</param>
    /// <param name="serialPort">COM-порт подключения.</param>
    /// <returns>Новый экземпляр устройства типа T.</returns>
    public static T CreateMeter<T>(DeviceEnum.Type type, string name, string description, string number, SerialPort serialPort) where T : BreakdownBase, new()
    {
      T meter = new T();
      meter.DeviceType = type;
      meter.Name = name;
      meter.Description = description;
      meter.Number = number;
      meter.Port = serialPort;
      return meter;
    }

    /// <summary>
    /// Выполняет поиск COM-порта устройства на основе указанных идентификаторов производителя (VID) и продукта (PID).
    /// </summary>
    /// <param name="vid">Идентификатор производителя устройства (VID) в формате строки.</param>
    /// <param name="pid">Идентификатор продукта устройства (PID) в формате строки.</param>
    /// <returns>Строка с именем COM-порта (например, "COM3") или null, если устройство не найдено.</returns>
    public static string FindComPort(string vid, string pid)
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
  }
}
