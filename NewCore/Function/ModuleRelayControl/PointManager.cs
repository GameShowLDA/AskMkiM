using System.Diagnostics;
using System.Net;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static NewCore.Enum.DeviceEnum;
using static AppConfiguration.Execution.ExecutionConfig;
using NewCore.Base.DeviceResponses;
using static Utilities.LoggerUtility;

namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет точками (реле) модуля коммутации реле (МКР).
  /// </summary>
  public class PointManager : IPointManager
  {
    /// <summary>
    /// Экземпляр интерфейса модуля реле.
    /// </summary>
    private readonly IRelaySwitchModule _moduleRelayControl;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="PointManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public PointManager(IRelaySwitchModule moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    /// <summary>
    /// Подключает точку (реле) МКР к указанной шине.
    /// </summary>
    /// <param name="bus">Шина, к которой подключается реле.</param>
    /// <param name="number">Номер точки (реле).</param>
    /// <returns>Возвращает <c>true</c>, если команда успешно отправлена.</returns>
    public async Task<bool> ConnectRelayAsync(BusPoint bus, int number)
    {
      if (await GetIsIdleModeEnabled())
        return true;

      var cmd = new DeviceCommand(8, number, (int)bus, 1);
      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 1000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"8.{number}.{(int)bus}.1")
          return true;

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
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number)
    {
      if (await GetIsIdleModeEnabled())
        return true;

      var cmd = new DeviceCommand(8, number, (int)bus, 2);
      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 1000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"8.{number}.{(int)bus}.2")
          return true;

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
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint)
    {
      if (await GetIsIdleModeEnabled())
        return true;

      DeviceCommand cmd = new DeviceCommand
      {
        Number = 11,
        FirstParameter = firstPoint,
        SecondParameter = lastPoint,
        ThirdParameter = (int)bus * 10 + 1,
      };

      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 3000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"11.{firstPoint}.{lastPoint}.{(int)bus * 10 + 1}")
          return true;

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
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint)
    {
      if (await GetIsIdleModeEnabled())
        return true;

      DeviceCommand cmd = new DeviceCommand
      {
        Number = 11,
        FirstParameter = firstPoint,
        SecondParameter = lastPoint,
        ThirdParameter = (int)bus * 10 + 2
      };

      string commandText = cmd.ToString();

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 3000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"11.{firstPoint}.{lastPoint}.{(int)bus * 10 + 2}")
        {
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
    public async Task<string> CheckPoint(int numberPoint)
    {
      if (await GetIsIdleModeEnabled())
      {
        return "Включен холостой режим.";
      }

      var cmd = new DeviceCommand(6, numberPoint);
      string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      return response;
    }
  }
}
