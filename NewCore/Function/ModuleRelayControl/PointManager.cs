using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Base.DeviceResponses;
using NewCore.Communication;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет точками (реле) модуля коммутации реле (МКР).
  /// </summary>
  public class PointManager : IPointManager
  {
    private Dictionary<int, bool> IsConnectedPointBusA = new Dictionary<int, bool>();
    private Dictionary<int, bool> IsConnectedPointBusB = new Dictionary<int, bool>();

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
      _moduleRelayControl.ConnectableManager.IsReset += ConnectableManager_IsReset;
      ConnectableManager_IsReset();
    }

    private void ConnectableManager_IsReset()
    {
      int countPoint = _moduleRelayControl.PointCount;
      IsConnectedPointBusA.Clear();
      IsConnectedPointBusB.Clear();

      for (int i = 1; i <= countPoint; i++)
      {
        IsConnectedPointBusA.Add(i, false);
        IsConnectedPointBusB.Add(i, false);
      }
    }

    /// <summary>
    /// Подключает точку (реле) МКР к указанной шине.
    /// </summary>
    /// <param name="bus">Шина, к которой подключается реле.</param>
    /// <param name="number">Номер точки (реле).</param>
    /// <returns>Возвращает <c>true</c>, если команда успешно отправлена.</returns>
    public async Task<bool> ConnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      if (CheckPointConnected(number, bus, true))
      {
        return true;
      }

      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (bus == BusPoint.A)
        {
          IsConnectedPointBusA[number] = true;
        }
        else
        {
          IsConnectedPointBusB[number] = true;
        }

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
          if (bus == BusPoint.A)
          {
            IsConnectedPointBusA[number] = true;
          }
          else
          {
            IsConnectedPointBusB[number] = true;
          }

          return true;
        }

        LogWarning($"Ответ на команду подключения точки {number} не получен или некорректный. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось подключить точку {number} к шине {bus}.", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Отключает точку (реле) МКР от указанной шины.
    /// </summary>
    /// <param name="bus">Шина, от которой отключается реле.</param>
    /// <param name="number">Номер точки (реле).</param>
    /// <returns>Возвращает <c>true</c>, если команда успешно отправлена.</returns>
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null)
    {
      if (CheckPointConnected(number, bus, false))
      {
        return true;
      }

      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (bus == BusPoint.A)
        {
          IsConnectedPointBusA[number] = false;
        }
        else
        {
          IsConnectedPointBusB[number] = false;
        }

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
          if (bus == BusPoint.A)
          {
            IsConnectedPointBusA[number] = false;
          }
          else
          {
            IsConnectedPointBusB[number] = false;
          }

          return true;
        }

        LogWarning($"Ответ на команду отключения точки {number} не получен или некорректный. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось отключить точку {number} от шины {bus}.", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Подключает диапазон точек (реле) МКР к указанной шине.
    /// </summary>
    /// <param name="bus">Шина, к которой подключается диапазон реле.</param>
    /// <param name="firstPoint">Первая точка в диапазоне.</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>Возвращает <c>true</c>, если команда выполнена успешно.</returns>
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
        return true;

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
          for (int number = firstPoint; number <= lastPoint; number++)
          {
            if (bus == BusPoint.A)
              IsConnectedPointBusA[number] = true;
            else
              IsConnectedPointBusB[number] = true;
          }

          return true;
        }

        LogWarning($"Ответ на команду подключения диапазона точек {firstPoint}-{lastPoint} не получен или некорректен. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось подключить диапазон точек {firstPoint}-{lastPoint} к шине {bus}.", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Отключает диапазон точек (реле) МКР от указанной шины.
    /// </summary>
    /// <param name="bus">Шина, от которой отключается диапазон реле.</param>
    /// <param name="firstPoint">Первая точка в диапазоне.</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>Возвращает <c>true</c>, если команда выполнена успешно.</returns>
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
        return true;

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
          for (int number = firstPoint; number <= lastPoint; number++)
          {
            if (bus == BusPoint.A)
              IsConnectedPointBusA[number] = false;
            else
              IsConnectedPointBusB[number] = false;
          }

          return true;
        }

        LogWarning($"Ответ на команду отключения диапазона точек {firstPoint}-{lastPoint} не получен или некорректен. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError($"Не удалось отключить диапазон точек {firstPoint}-{lastPoint} от шины {bus}.", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Проверяет работоспособность точки (реле) в модуле МКР.
    /// </summary>
    /// <param name="numberPoint">Номер проверяемой точки.</param>
    /// <returns>Строка с ответом от устройства.</returns>
    public async Task<string> CheckPoint(int numberPoint, IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
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
      if (CheckPointConnected(nubmerPoint, bus, true))
      {
        return true;
      }

      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (bus == BusPoint.A)
        {
          IsConnectedPointBusB[nubmerPoint] = false;
          IsConnectedPointBusA[nubmerPoint] = true;
        }
        else
        {
          IsConnectedPointBusA[nubmerPoint] = false;
          IsConnectedPointBusB[nubmerPoint] = true;
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
          IsConnectedPointBusB[nubmerPoint] = false;
          IsConnectedPointBusA[nubmerPoint] = true;
        }
        else
        {
          IsConnectedPointBusA[nubmerPoint] = false;
          IsConnectedPointBusB[nubmerPoint] = true;
        }
      }
      return result;
    }

    public async Task<bool> DisconnectingAllPoint(IUserInteractionService? userMessageService = null)
    {
      bool success = true;

      foreach (var kv in IsConnectedPointBusA.ToList())
      {
        if (kv.Value)
        {
          var result = await DisconnectRelayAsync(BusPoint.A, kv.Key, userMessageService);
          if (!result)
            success = false;
        }
      }

      foreach (var kv in IsConnectedPointBusB.ToList())
      {
        if (kv.Value)
        {
          var result = await DisconnectRelayAsync(BusPoint.B, kv.Key, userMessageService);
          if (!result)
            success = false;
        }
      }

      return success;
    }

    private bool CheckPointConnected(int number, BusPoint busPoint, bool connect)
    {
      if (busPoint == BusPoint.A)
      {
        IsConnectedPointBusA.TryGetValue(number, out bool connected);
        if (connected == connect)
        {
          return true;
        }
      }
      else
      {
        IsConnectedPointBusB.TryGetValue(number, out bool connected);
        if (connected == connect)
        {
          return true;
        }
      }

      return false;
    }

    public IReadOnlyList<PointConnectionInfo> GetConnectedPoints()
    {
      var result = new List<PointConnectionInfo>();

      foreach (var kvp in IsConnectedPointBusA)
      {
        if (kvp.Value)
          result.Add(new PointConnectionInfo(kvp.Key, BusPoint.A));
      }

      foreach (var kvp in IsConnectedPointBusB)
      {
        if (kvp.Value)
          result.Add(new PointConnectionInfo(kvp.Key, BusPoint.B));
      }

      return result;
    }
  }
}
