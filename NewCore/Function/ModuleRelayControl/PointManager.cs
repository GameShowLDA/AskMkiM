using System.Diagnostics;
using System.Net;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static NewCore.Enum.DeviceEnum;

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
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), new DeviceCommand(8, number, (int)bus, 1));
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Отключает точку (реле) МКР от указанной шины.
    /// </summary>
    /// <param name="bus">Шина, от которой отключается реле.</param>
    /// <param name="number">Номер точки (реле).</param>
    /// <returns>Возвращает <c>true</c>, если команда успешно отправлена.</returns>
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number)
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), new DeviceCommand(8, number, (int)bus, 2));
      await Task.Delay(5);
      return true;
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
      DeviceCommand command = new DeviceCommand
      {
        Number = 11,
        FirstParameter = firstPoint,
        SecondParameter = lastPoint,
        ThirdParameter = (1 * 10) + (int)bus,
      };

      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      string answer = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), command, 3000);
      stopwatch.Stop();
      Console.WriteLine($"Время ожидания: {stopwatch.Elapsed}");

      return answer.Contains("11.1");
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
      DeviceCommand command = new DeviceCommand
      {
        Number = 11,
        FirstParameter = firstPoint,
        SecondParameter = lastPoint,
        ThirdParameter = (2 * 10) + (int)bus,
      };

      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), command);
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Проверяет работоспособность точки (реле) в модуле МКР.
    /// </summary>
    /// <param name="numberPoint">Номер проверяемой точки.</param>
    /// <returns>Строка с ответом от устройства.</returns>
    public async Task<string> CheckPoint(int numberPoint)
    {
      return await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), new DeviceCommand(6, numberPoint), 1000);
    }
  }
}
