using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Function.ModuleRelayControl
{
  public class PointManager : IPointManager
  {
    IRelaySwitchModule _moduleRelayControl { get; set; }
    public PointManager(IRelaySwitchModule moduleRelayControl) => _moduleRelayControl = moduleRelayControl;


    /// <summary>
    /// Подключить точку(реле) МКР.
    /// </summary>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки(реле).</param>
    /// <returns> Возвращает объект типа Task.</returns>
    public async Task<bool> ConnectRelayAsync(BusPoint bus, int number)
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), new DeviceCommand(8, number, (int)bus, 1));
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Отключить точку(реле) МКР.
    /// </summary>
    /// <param name="_moduleRelayControl.IPAddress">IP МКР.</param>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки(реле).</param>
    /// <returns> Возвращает объект типа Task.</returns>
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number)
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), new DeviceCommand(8, number, (int)bus, 2));
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Подключение диапазона точек МКР.
    /// </summary>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firtsPoint">Первая тоска в диапазоне</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>Результат подключения.</returns>
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firtsPoint, int lastPoint)
    {
      DeviceCommand command = new DeviceCommand();
      command.Number = 11;
      command.FirstParameter = firtsPoint;
      command.SecondParameter = lastPoint;
      command.ThirdParameter = (1 * 10) + (int)bus;

      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      string answer = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), command, 3000);
      stopwatch.Stop();
      Console.WriteLine($"Время ожидания: {stopwatch.Elapsed}");
      if (answer.Contains("11.1"))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Отключение диапазона точек МКР.
    /// </summary>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firtsPoint">Первая тоска в диапазоне</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>Результат подключения.</returns>
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firtsPoint, int lastPoint)
    {
      DeviceCommand command = new DeviceCommand();
      command.Number = 11;
      command.FirstParameter = firtsPoint;
      command.SecondParameter = lastPoint;
      command.ThirdParameter = (2 * 10) + (int)bus;
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), command);
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Проверяет точку на работоспособность у МКР.
    /// </summary>
    /// <param name="numberPoint">Номер точки.</param>
    /// <returns>Возвращает ответ от устрйоства.</returns>
    public async Task<string> CheckPoint(int numberPoint)
    {
      return await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), new DeviceCommand(6, numberPoint), 1000);
    }
  }
}
