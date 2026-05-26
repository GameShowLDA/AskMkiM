using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.DeviceResponses;
using Ask.Device.Runtime.Commands;
using Ask.Device.Runtime.Ethernet.Udp.Broadcast;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет точками (реле) модуля коммутации реле (МКР).
  /// </summary>
  public class PointManager : IPointManager
  {
    private readonly PointConnectionStateStore connectionState;

    /// <summary>
    /// Экземпляр интерфейса модуля реле.
    /// </summary>
    private readonly IRelaySwitchModule _moduleRelayControl;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="PointManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public PointManager(IRelaySwitchModule moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl;
      connectionState = new PointConnectionStateStore(moduleRelayControl.PointCount);
      _moduleRelayControl.ConnectableManager.IsReset += ConnectableManager_IsReset;
      UdpBroadcastCommandSender.ResetAllDevicesSent += ConnectableManager_IsReset;
      ConnectableManager_IsReset();
    }

    private void ConnectableManager_IsReset()
    {
      connectionState.Reset(_moduleRelayControl.PointCount);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        SetPointConnection(number, bus, true);
        return true;
      }

      var cmd = new DeviceCommand(8, number, (int)bus, 1);
      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 1000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"8.{number}.{(int)bus}.1")
        {
          SetPointConnection(number, bus, true);
          return true;
        }

        LogWarning($"Ответ на команду подключения точки {number} не получен или некорректный. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось подключить точку {number} к шине {bus}.", isDeviceLog: true);
      SetPointConnection(number, bus, false);
      return false;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        SetPointConnection(number, bus, false);
        return true;
      }

      var cmd = new DeviceCommand(8, number, (int)bus, 2);
      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 1000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"8.{number}.{(int)bus}.2")
        {
          SetPointConnection(number, bus, false);
          return true;
        }

        LogWarning($"Ответ на команду отключения точки {number} не получен или некорректный. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось отключить точку {number} от шины {bus}.", isDeviceLog: true);
      SetPointConnection(number, bus, false);
      return false;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayVerifiedAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        SetPointConnection(number, bus, true);
        return true;
      }

      var cmd = new DeviceCommand(82, number, (int)bus, 1);
      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 1000);
        var parsed = RelayVerifiedAnswer.FromJson(response);

        if (parsed != null && parsed.Checked)
        {
          SetPointConnection(number, bus, true);
          return true;
        }

        LogWarning($"Ответ на команду подключения точки {number} не получен или некорректный. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось подключить точку {number} к шине {bus}.", isDeviceLog: true);
      SetPointConnection(number, bus, false);
      return false;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayVerifiedAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        SetPointConnection(number, bus, false);
        return true;
      }

      var cmd = new DeviceCommand(82, number, (int)bus, 2);
      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 1000);
        var parsed = RelayVerifiedAnswer.FromJson(response);

        if (parsed != null && parsed.Checked)
        {
          SetPointConnection(number, bus, false);
          return true;
        }

        LogWarning($"Ответ на команду отключения точки {number} не получен или некорректный. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось отключить точку {number} от шины {bus}.", isDeviceLog: true);
      SetPointConnection(number, bus, false);
      return false;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        connectionState.SetRange(firstPoint, lastPoint, bus, true);
        return true;
      }

      DeviceCommand cmd = new DeviceCommand
      {
        Number = 11,
        FirstParameter = firstPoint,
        SecondParameter = lastPoint,
        ThirdParameter = ((int)bus * 10) + 1,
      };

      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 3000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"11.{firstPoint}.{lastPoint}.{((int)bus * 10) + 1}")
        {
          connectionState.SetRange(firstPoint, lastPoint, bus, true);
          return true;
        }

        LogWarning($"Ответ на команду подключения диапазона точек {firstPoint}-{lastPoint} не получен или некорректен. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось подключить диапазон точек {firstPoint}-{lastPoint} к шине {bus}.", isDeviceLog: true);
      connectionState.SetRange(firstPoint, lastPoint, bus, false);
      return false;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        connectionState.SetRange(firstPoint, lastPoint, bus, false);
        return true;
      }

      DeviceCommand cmd = new DeviceCommand
      {
        Number = 11,
        FirstParameter = firstPoint,
        SecondParameter = lastPoint,
        ThirdParameter = ((int)bus * 10) + 2
      };

      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 3000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"11.{firstPoint}.{lastPoint}.{((int)bus * 10) + 2}")
        {
          connectionState.SetRange(firstPoint, lastPoint, bus, false);
          return true;
        }

        LogWarning($"Ответ на команду отключения диапазона точек {firstPoint}-{lastPoint} не получен или некорректен. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось отключить диапазон точек {firstPoint}-{lastPoint} от шины {bus}.", isDeviceLog: true);
      connectionState.SetRange(firstPoint, lastPoint, bus, false);
      return false;
    }

    /// <inheritdoc />
    public async Task<string> CheckPoint(int numberPoint, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return "Включен холостой режим.";
      }

      var cmd = new DeviceCommand(6, numberPoint);
      string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      return response;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectingPointToNewBus(BusPoint bus, int nubmerPoint, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (bus == BusPoint.A)
        {
          SetPointConnection(nubmerPoint, BusPoint.B, false);
          SetPointConnection(nubmerPoint, BusPoint.A, true);
        }
        else
        {
          SetPointConnection(nubmerPoint, BusPoint.A, false);
          SetPointConnection(nubmerPoint, BusPoint.B, true);
        }
        return true;
      }

      var cmd = new DeviceCommand(81, nubmerPoint, (int)bus);
      string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      var result = response.Contains(cmd.ToString()[..^1]);
      if (result)
      {
        if (bus == BusPoint.A)
        {
          SetPointConnection(nubmerPoint, BusPoint.B, false);
          SetPointConnection(nubmerPoint, BusPoint.A, true);
        }
        else
        {
          SetPointConnection(nubmerPoint, BusPoint.A, false);
          SetPointConnection(nubmerPoint, BusPoint.B, true);
        }
      }
      else
      {
        SetPointConnection(nubmerPoint, BusPoint.AB, false);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectingAllPoint(IUserInteractionService? userMessageService = null)
    {
      bool success = true;

      foreach (int number in connectionState.GetConnectedPointNumbers(BusPoint.A))
      {
        var result = await DisconnectRelayAsync(BusPoint.A, number, userMessageService);
        if (!result)
          success = false;
      }

      foreach (int number in connectionState.GetConnectedPointNumbers(BusPoint.B))
      {
        var result = await DisconnectRelayAsync(BusPoint.B, number, userMessageService);
        if (!result)
          success = false;
      }

      return success;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectingAllPointFromBusA(IUserInteractionService? userMessageService = null)
    {
      bool success = true;

      foreach (int number in connectionState.GetConnectedPointNumbers(BusPoint.A))
      {
        var result = await DisconnectRelayAsync(BusPoint.A, number, userMessageService);
        if (!result)
          success = false;
      }

      return success;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectingAllPointFromBusB(IUserInteractionService? userMessageService = null)
    {
      bool success = true;

      foreach (int number in connectionState.GetConnectedPointNumbers(BusPoint.B))
      {
        var result = await DisconnectRelayAsync(BusPoint.B, number, userMessageService);
        if (!result)
          success = false;
      }

      return success;
    }

    private void SetPointConnection(int number, BusPoint busPoint, bool connected)
    {
      connectionState.Set(number, busPoint, connected);
    }

    /// <inheritdoc />
    public IReadOnlyList<PointConnectionInfo> GetConnectedPoints()
    {
      return connectionState.GetConnectedPoints();
    }
  }
}
