using System.IO.Ports;
using System.Management;
using System.Security.Cryptography;
using System.Xml.Linq;
using NewCore.Base.Device;
using NewCore.Device;
using NewCore.Function.GPT.Data;
using Utilities.Interface;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.LoggerUtility;

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

    public event Action DeviceDisponce;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserMessageService messageService = null)
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
        if (!_gptModel.COMPort.IsOpen)
        {
          _gptModel.COMPort.DtrEnable = true;
          _gptModel.COMPort.RtsEnable = true;
          _gptModel.COMPort.NewLine = "\r\n";
          _gptModel.COMPort.ReadTimeout = 2000;
          _gptModel.COMPort.WriteTimeout = 2000;

          _gptModel.COMPort.Open();
          LogInformation($"[{_gptModel.Name}] Порт {_gptModel.COMPort.PortName} успешно открыт.", isDeviceLog: true);
        }

        return (true, string.Empty);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка при открытии порта {_gptModel.COMPort.PortName}", ex, isDeviceLog: true);
        return (false, ex.Message);
      }
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserMessageService messageService = null)
    {
      if (await GetIsIdleModeEnabled())
      {
        return true;
      }

      try
      {
        if (_gptModel.COMPort != null && _gptModel.COMPort.IsOpen)
        {
          _gptModel.COMPort.Close();
          LogInformation($"[{_gptModel.Name}] Порт {_gptModel.COMPort.PortName} закрыт.", isDeviceLog: true);
        }
      }
      catch (Exception ex)
      {
        LogException($"Ошибка при закрытии порта {_gptModel.Name}", ex, isDeviceLog: true);
      }

      return true;
    }


    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserMessageService messageService = null)
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, "Включен холостой режим");
      }

      return (true, "Включен холостой режим");
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserMessageService messageService = null)
    {
      return true;
    }
  }
}
