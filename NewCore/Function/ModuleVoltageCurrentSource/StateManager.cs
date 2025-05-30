using System.Net;
using NewCore.Base.Device;
using NewCore.Base.DeviceResponses;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Класс для управления состоянием модуля источника напряжения и тока (МИНТ).
  /// </summary>
  public class StateManager : IConnectable
  {
    private readonly IPowerSourceModule _moduleVoltageCurrentSource;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="moduleVoltageCurrentSource">Модуль источника напряжения и тока, для которого будет выполняться управление состоянием.</param>
    public StateManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return (true, "Включен холостой режим");
      }

      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);

      // Десериализация ответа
      BaseResponse baseResponse = BaseResponse.FromJson(result);
      if (baseResponse != null)
      {
        // Проверка на соответствие номеру шасси и устройства
        if (baseResponse.NumberChassis == _moduleVoltageCurrentSource.NumberChassis &&
            baseResponse.NumberDevice == _moduleVoltageCurrentSource.Number)
        {
          return (true, result);
        }
        else
        {
          string errorMessage = string.Empty;

          // Сообщения об ошибках, если номера не совпадают
          if (baseResponse.NumberChassis != _moduleVoltageCurrentSource.NumberChassis)
          {
            errorMessage += $"Несовпадение по NumberChassis: ожидается {_moduleVoltageCurrentSource.NumberChassis}, получено {baseResponse.NumberChassis}. ";
          }

          if (baseResponse.NumberDevice != _moduleVoltageCurrentSource.Number)
          {
            errorMessage += $"Несовпадение по NumberDevice: ожидается {_moduleVoltageCurrentSource.Number}, получено {baseResponse.NumberDevice}.";
          }

          return (false, errorMessage.Trim());
        }
      }

      return (false, result);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(2, 1, 0, 0);
      string result = await _moduleVoltageCurrentSource.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      return result == "2.0.1";
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      return await InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      await _moduleVoltageCurrentSource.DeviceProtocol.OperationLock.WaitAsync();
      return await ResetAsync();
    }
  }
}