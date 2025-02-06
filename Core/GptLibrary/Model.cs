using System.IO.Ports;
using System.Text;
using Core.Abstract;
using static Utilities.LoggerUtility;

namespace Core.GptLibrary
{
  /// <summary>
  /// Модель данных GPT.
  /// </summary>
  public class Model : BreakdownBase
  {
    private const string VID = "10C4";
    private const string PID = "EA60";

    /// <summary>
    /// Создаёт модель GPT-79904 с попыткой подключения.
    /// </summary>
    /// <returns></returns>
    public static Model CreateAsync()
    {
      Model model;
      Connect(out model);
      model.DeviceType = Enum.DeviceEnum.Type.Breakdown;
      model.Name = "GPT79904";
      return model;
    }

    /// <summary>
    /// Проверяет соединение с устройством.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="bool"/>, указывающий на наличие соединения.
    /// </returns>
    public override bool CheckConnection()
    {
      try
      {
        LogInformation("Открываем порт...");
        if (this.Port != null)
        {
          using (var port = new SerialPort(this.Port.PortName, this.Port.BaudRate, this.Port.Parity, this.Port.DataBits, this.Port.StopBits))
          {
            port.Open();
            LogInformation("Порт открыт");

            Thread.Sleep(100);
            port.DiscardInBuffer();
            port.DiscardOutBuffer();

            LogInformation("Отправляем команду идентификации...");
            port.Write("*IDN?\r\n");
            Thread.Sleep(100);

            if (port.BytesToRead > 0)
            {
              byte[] buffer = new byte[100];
              int bytesRead = port.Read(buffer, 0, buffer.Length);
              string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

              LogInformation($"Получен ответ: {response.Trim()}");
              return response.StartsWith("GPT-");
            }

            LogError("Ответ не получен");
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
    /// Подключение к устройству пробойки.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="bool"/>, указывающий на успех подключения.
    /// </returns>
    public override bool Connect()
    {
      string portName = FindComPort(VID, PID);
      LogInformation($"Найден порт: {portName}");

      if (string.IsNullOrEmpty(portName))
      {
        LogError("COM порт не найден");
        this.Port = null;
        return false;
      }

      try
      {
        this.Port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
        {
          ReadTimeout = 2000,
          WriteTimeout = 2000,
          DtrEnable = true,
          RtsEnable = true,
          Handshake = Handshake.None
        };

        return this.CheckConnection();
      }
      catch (Exception ex)
      {
        LogError($"Ошибка: {ex.Message}");
        this.Port = null;
        return false;
      }
    }

    /// <summary>
    /// Подключение к устройству пробойки.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="bool"/>, указывающий на успех подключения.
    /// </returns>
    public static bool Connect(out Model model)
    {
      model = new Model();
      string portName = FindComPort(VID, PID);
      LogInformation($"Найден порт: {portName}");

      if (string.IsNullOrEmpty(portName))
      {
        LogError("COM порт не найден");
        model.Port = null;
        return false;
      }

      try
      {
        model.Port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
        {
          ReadTimeout = 2000,
          WriteTimeout = 2000,
          DtrEnable = true,
          RtsEnable = true,
          Handshake = Handshake.None
        };
        model.ModuleActive = true;
        return true;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка: {ex.Message}");
        model.Port = null;
        return false;
      }
    }


    /// <summary>
    /// Разрывает соединение с устройством и освобождает ресурсы порта.
    /// </summary>
    public override void Disconnect()
    {
      if (this.Port != null)
      {
        try
        {
          if (this.Port.IsOpen)
          {
            this.Port.Close();
            LogInformation("Порт закрыт.");
          }
        }
        catch (Exception ex)
        {
          LogError($"Ошибка при закрытии порта: {ex.Message}");
        }
        finally
        {
          this.Port.Dispose();
          this.Port = null;
          LogInformation("Ресурсы порта освобождены.");
        }
      }
    }

    /// <summary>
    /// Чтение порта.
    /// </summary>
    /// <returns>Ответ с устройства</returns>
    public override async Task<string> ReadLineAsync()
    {
      try
      {
        if (!this.Port.IsOpen)
        {
          this.Port.Open();
        }

        this.Port.ReadTimeout = 5000;

        if (this.Port.BytesToRead > 0)
        {
          return await Task.Run(() => this.Port.ReadLine());
        }
        else
        {
          Console.WriteLine("Нет данных для чтения.");
          return string.Empty;
        }
      }
      catch (TimeoutException ex)
      {
        Console.WriteLine($"Таймаут при чтении из порта: {ex.Message}");
        return string.Empty;
      }
      catch (IOException ex)
      {
        Console.WriteLine($"Ошибка ввода-вывода при чтении из порта: {ex.Message}");
        return string.Empty;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Неизвестная ошибка при чтении из порта: {ex.Message}");
        return string.Empty;
      }
    }

    /// <summary>
    /// Отправка на сообщения в порт.
    /// </summary>
    /// <param name="command">Отправляемое сообщение.</param>
    public override async Task WriteLineAsync(string command)
    {
      try
      {
        LogInformation($"Отправка команды на GPT79904: {command}");

        if (!this.Port.IsOpen)
        {
          this.Port.Open();
        }

        this.Port.WriteLine(command);
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
