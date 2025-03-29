using NewCore.Base.Device;
using NewCore.Device;
using System.IO.Ports;
using System.Management;
using System.Security.Cryptography;
using System.Xml.Linq;
using static Utilities.LoggerUtility;
using static AppConfiguration.Execution.ExecutionConfig;
using NewCore.Function.GPT.Data;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для управления состоянием пробойной установки.
  /// </summary>
  public class ConnectableManager : IConnectable
  {
    /// <summary>
    /// Создает новый экземпляр класса <see cref="AcwMode"/>.
    /// </summary>
    /// <param name="gpt79904">Объект устройства GPT-79904.</param>
    public ConnectableManager(GPT79904 gpt79904) => _gptModel = gpt79904;

    /// <summary>
    /// Модель устройства GPT-79904.
    /// </summary>
    private GPT79904 _gptModel { get; set; }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, "Включен холостой режим");
      }

      if (_gptModel.COMPort == null)
      {
        return (false, "COM-порт не инициализирован.");
      }

      try
      {
        if (_gptModel.COMPort.BaudRate == 0)
        {
          _gptModel.COMPort.BaudRate = 9600; // Скорость передачи данных
        }

        if (_gptModel.COMPort.Parity == Parity.None)
        {
          _gptModel.COMPort.Parity = Parity.None; // Четность
        }

        if (_gptModel.COMPort.DataBits == 0)
        {
          _gptModel.COMPort.DataBits = 8; // Количество бит данных
        }

        if (_gptModel.COMPort.StopBits == StopBits.None)
        {
          _gptModel.COMPort.StopBits = StopBits.One; // Стоповые биты
        }

        if (!_gptModel.COMPort.IsOpen)
        {
          _gptModel.COMPort.Open();
        }

        LogInformation($"Успешно подключено к устройству {_gptModel.Name} через COM-порт {_gptModel.COMPort.PortName}");
      }
      catch (UnauthorizedAccessException ex)
      {
        LogError($"Ошибка доступа к COM-порту {_gptModel.COMPort.PortName}: {ex.Message}");
        return (false, $"Ошибка доступа к COM-порту {_gptModel.COMPort.PortName}: {ex.Message}");
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при подключении к устройству {_gptModel.Name}: {ex.Message}");
        return (false, $"Ошибка при подключении к устройству {_gptModel.Name}: {ex.Message}");
        throw;
      }

      return (true, string.Empty);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return true;
      }

      if (_gptModel.COMPort == null)
      {
        return true;
      }

      try
      {
        if (_gptModel.COMPort.IsOpen)
        {
          _gptModel.COMPort.Close();
        }
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при отключении от устройства {_gptModel.Name}: {ex.Message}");
      }

      return true;
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, "Включен холостой режим");
      }

      try
      {
        LogInformation("Открываем порт...");
        if (_gptModel.COMPort != null)
        {
          using (var port = _gptModel.COMPort)
          {
            await Task.Run(() => port.Open());
            LogInformation("Порт открыт");

            string query = $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%({_gptModel.COMPort})%'";
            using (var searcher = new ManagementObjectSearcher(query))
            {
              var results = await Task.Run(() => searcher.Get().Cast<ManagementObject>().ToList());

              foreach (var obj in results)
              {
                string deviceID = obj["DeviceID"]?.ToString() ?? string.Empty;
                if (deviceID.Contains(_gptModel.VID) && deviceID.Contains(_gptModel.PID))
                {
                  LogInformation($"Устройство найдено по VID/PID: {_gptModel.VID}, {_gptModel.PID}");
                  return (true, string.Empty);
                }
              }
            }

            LogError("Устройство не найдено по VID/PID");
            return (false, "Устройство не найдено по VID/PID");
          }
        }

        return (false, "COM порт не иинициализирован");
      }
      catch (UnauthorizedAccessException ex)
      {
        LogError($"Ошибка доступа к порту: {ex.Message}");
        return (false, $"Ошибка доступа к порту: {ex.Message}");
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при проверке соединения: {ex.Message}");
        return (false, $"Ошибка при проверке соединения: {ex.Message}");
      }
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync()
    {
      return true;
    }
  }
}
