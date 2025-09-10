using System.Net.Http.Headers;
using AppConfiguration.Theme;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandExecutor.Execution;
using Utilities;
using Utilities.Interface;
using Utilities.Models;
using static ControlCommandExecutor.BaseStrategies.NodeFullChecker;

namespace ControlCommandExecutor.BaseStrategies
{
  internal static class MethodExecutor
  {
    /// <summary>
    /// Количество разрядов в двоичном представлении номера точки.
    /// </summary>
    static int HighestBitCount { get; set; }

    /// <summary>
    /// Выполняет последовательную проверку точек групповым методом.
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task CheckSequenceAsync(SchemeModel schemeModel, PerformMeasurementAsync performMeasurementAsync, CommandExecutionManager manager, BaseCommandModel siCommandModel, IUserMessageService messageService, double resistance)
    {

      var pointsList = schemeModel.GetPointsDisconnected();
      if (pointsList.Count == 0)
      {
        return;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка разобщённых точек"));


      List<ChainModel> chains = new List<ChainModel>();
      foreach (var point in pointsList)
      {
        chains.Add(new ChainModel(point));
      }

      HighestBitCount = GetHighestPointBinaryDigits(chains);
      var binaryPoints = ConvertToReversedBinaryRange(chains, HighestBitCount);


      for (int step = 0; step < HighestBitCount; step++)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка разряда {ConvertIntToString(step + 1)} ({HighestBitCount})"), IsBlockStart: true);
        await ConnectPointsToBusAsync(binaryPoints, schemeModel, step, messageService);
        if (!(await performMeasurementAsync(resistance, messageService, messageService.GetCancellationToken())).Result)
        {
          await DisconnectPointsToBusAsync(binaryPoints, schemeModel, step, messageService);
          await messageService.ShowMessageAsync(new ShowMessageModel($"Ошибка при проверке разряда {step} ({HighestBitCount})", type: ShowMessageModel.MessageType.Error), IsBlockStart: true);

          await messageService.ShowMessageAsync(new ShowMessageModel($"Выполение измерения методом полного узла"), IsBlockStart: true);
          await BaseStrategies.NodeFullChecker.CheckSequenceAsync(schemeModel, performMeasurementAsync, manager, siCommandModel, messageService, resistance);

          return;
        }
        await DisconnectPointsToBusAsync(binaryPoints, schemeModel, step, messageService);
      }
    }

    /// <summary>
    /// Подключает указанную точку к шине B через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо подключить к шине B.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    /// </exception>
    private static async Task ConnectToBusBAsync(ChainModel points, IUserMessageService messageService)
    {
      foreach (var point in points.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }

    /// <summary>
    /// Отключает указанную точку от шины B через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    private static async Task DisconnectFromBusBAsync(ChainModel points, IUserMessageService messageService)
    {
      foreach (var point in points.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.DisconnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }

    /// <summary>
    /// Подключает указанную точку к шине A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо подключить к шине A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    /// </exception>
    private static async Task ConnectToBusAAsync(ChainModel points, IUserMessageService messageService)
    {
      foreach (var point in points.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }

    /// <summary>
    /// Отключает указанную точку от шины A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    private static async Task DisconnectFromBusAAsync(ChainModel points, IUserMessageService messageService)
    {
      foreach (var point in points.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.DisconnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }

    /// <summary>
    /// Возвращает количество разрядов в двоичном представлении наибольшего номера точки в диапазоне.
    /// </summary>
    /// <param name="startPoint">Начальная точка диапазона.</param>
    /// <param name="endPoint">Конечная точка диапазона.</param>
    /// <returns>Количество битов в представлении наибольшего номера точки.</returns>
    static public int GetHighestPointBinaryDigits(List<ChainModel> points)
    {

      var maxPoints = points.Count;
      return Convert.ToString(maxPoints, 2).Length;
    }


    /// <summary>
    /// Преобразует все точки в диапазоне в перевёрнутые двоичные строки фиксированной длины.
    /// </summary>
    /// <param name="first">Первая точка диапазона.</param>
    /// <param name="second">Вторая точка диапазона.</param>
    /// <param name="bitLength">Желаемая длина двоичной строки.</param>
    /// <returns>Список точек и соответствующих перевёрнутых бинарных строк.</returns>
    static public List<(ChainModel point, string reversedBinary)> ConvertToReversedBinaryRange(
        List<ChainModel> points,
        int bitLength)
    {
      if (bitLength <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(bitLength), "Длина двоичной строки должна быть больше 0.");
      }

      var result = new List<(ChainModel point, string reversedBinary)>();
      string reversPoint = string.Empty;

      //foreach (var point in points)
      //{
      //  foreach (var item in point.PointModels)
      //  {
      //    reversPoint.Add(ConvertToReversedBinary(item.PointNumber, bitLength));
      //  }

      //  result.Add((point, reversPoint));
      //}

      for (int i = 1; i <= points.Count; i++)
      {
        var chain = points[i - 1];
        reversPoint = ConvertToReversedBinary(i, bitLength);
        result.Add((chain, reversPoint));
      }

      return result;
    }

    /// <summary>
    /// Преобразует число в двоичную строку заданной длины и переворачивает её.
    /// </summary>
    /// <param name="number">Число для преобразования.</param>
    /// <param name="bitLength">Желаемая длина строки.</param>
    /// <returns>Перевёрнутая двоичная строка.</returns>
    static private string ConvertToReversedBinary(int number, int bitLength)
    {
      string binary = Convert.ToString(number, 2).PadLeft(bitLength, '0');
      char[] array = binary.ToCharArray();
      Array.Reverse(array);
      return new string(array);
    }

    /// <summary>
    /// Подключает все точки группы к соответствующей шине в зависимости от текущего разряда.
    /// </summary>
    static private async Task ConnectPointsToBusAsync(List<(ChainModel point, string reversedBinary)> points, SchemeModel schemeModel, int step, IUserMessageService messageService)
    {
      foreach (var point in points)
      {
        if (point.reversedBinary[step] == '1')
        {
          await ConnectToBusAAsync(point.point, messageService);
        }
        else
        {
          await ConnectToBusBAsync(point.point, messageService);
        }

      }
    }


    /// <summary>
    /// Отключает все точки группы к соответствующей шине в зависимости от текущего разряда.
    /// </summary>
    static private async Task DisconnectPointsToBusAsync(List<(ChainModel point, string reversedBinary)> points, SchemeModel schemeModel, int step, IUserMessageService messageService)
    {

      foreach (var point in points)
      {
        if (point.reversedBinary[step] == '1')
        {
          await DisconnectFromBusAAsync(point.point, messageService);
        }
        else
        {
          await DisconnectFromBusBAsync(point.point, messageService);
        }
      }
    }

    /// <summary>
    /// Возвращает строку, в которой только текущий бит равен '1', а остальные — '0'.
    /// </summary>
    /// <param name="step">Текущий шаг (разряд), начиная с младшего.</param>
    /// <returns>Двоичная строка, где установлен только один бит.</returns>
    static public string GetBitString(int step)
    {
      var chars = Enumerable.Repeat('0', HighestBitCount).ToArray();
      chars[HighestBitCount - 1 - step] = '1';
      return new string(chars);
    }

    static private string ConvertIntToString(int number)
    {
      var str = string.Empty;
      for (int i = 0; i < HighestBitCount; i++)
      {
        if (HighestBitCount + 1 - number == i+1)
        {
          str += '1';
        }
        else
        {
          str += '0';
        }
      }

      return str;
    }
  }
}
