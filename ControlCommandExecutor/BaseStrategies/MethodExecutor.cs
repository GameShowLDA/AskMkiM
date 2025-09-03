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

    static List<ShowMessageModel> ResultMessages { get; set; }

    /// <summary>
    /// Выполняет последовательную проверку точек групповым методом.
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task CheckSequenceAsync(SchemeModel schemeModel, PerformMeasurementAsync performMeasurementAsync, CommandExecutionManager manager, BaseCommandModel siCommandModel, IUserMessageService messageService, double resistance)
    {
      List<PointModel> points = schemeModel.GetAllPoints();
      ResultMessages = new List<ShowMessageModel>();
      HighestBitCount = GetHighestPointBinaryDigits(points);
      var binaryPoints = ConvertToReversedBinaryRange(points, HighestBitCount);

      for (int step = 0; step < HighestBitCount; step++)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка разряда {step} ({HighestBitCount})"), IsBlockStart: true);
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

      await messageService.ShowMessageAsync(new ShowMessageModel("Результаты проверки") { IndentLevel = 1 });
      foreach (var messgae in ResultMessages)
      {
        await messageService.ShowMessageAsync(messgae, skipPause: true);
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
    private static async Task ConnectToBusBAsync(PointModel point, IUserMessageService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
      {
        throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
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
    private static async Task DisconnectFromBusBAsync(PointModel point, IUserMessageService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
      {
        throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.DisconnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
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
    private static async Task ConnectToBusAAsync(PointModel point, IUserMessageService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
      {
        throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
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
    private static async Task DisconnectFromBusAAsync(PointModel point, IUserMessageService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
      {
        throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.DisconnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
      }
    }

    /// <summary>
    /// Возвращает количество разрядов в двоичном представлении наибольшего номера точки в диапазоне.
    /// </summary>
    /// <param name="startPoint">Начальная точка диапазона.</param>
    /// <param name="endPoint">Конечная точка диапазона.</param>
    /// <returns>Количество битов в представлении наибольшего номера точки.</returns>
    static public int GetHighestPointBinaryDigits(List<PointModel> points)
    {
      var maxPoints = points.Select(p => (p.PointNumber)).Max();
      return Convert.ToString(maxPoints, 2).Length;
    }


    /// <summary>
    /// Преобразует все точки в диапазоне в перевёрнутые двоичные строки фиксированной длины.
    /// </summary>
    /// <param name="first">Первая точка диапазона.</param>
    /// <param name="second">Вторая точка диапазона.</param>
    /// <param name="bitLength">Желаемая длина двоичной строки.</param>
    /// <returns>Список точек и соответствующих перевёрнутых бинарных строк.</returns>
    static public List<(PointModel point, string reversedBinary)> ConvertToReversedBinaryRange(
        List<PointModel> points,
        int bitLength)
    {
      if (bitLength <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(bitLength), "Длина двоичной строки должна быть больше 0.");
      }

      var result = new List<(PointModel, string)>();

      foreach (var point in points)
      {
        result.Add((point, ConvertToReversedBinary(point.PointNumber, bitLength)));
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
    static private async Task ConnectPointsToBusAsync(List<(PointModel point, string reversedBinary)> points, SchemeModel schemeModel, int step, IUserMessageService messageService)
    {
      foreach (var point in points)
      {
        if (point.reversedBinary[step] == '1')
        {
          if (schemeModel.TryCommunicatedPointAllChain(point.point, out List<PointModel> result))
          {
            if (point.point.PointNumber != result[0].PointNumber)
            {
              continue;
            }

            foreach (var pointPair in result)
            {
              await ConnectToBusAAsync(pointPair, messageService);
            }
          }
          else
          {
            await ConnectToBusAAsync(point.point, messageService);
          }

        }
        else
        {
          if (schemeModel.TryCommunicatedPointAllChain(point.point, out List<PointModel> result))
          {
            if (point.point.PointNumber != result[0].PointNumber)
            {
              continue;
            }

            foreach (var pointPair in result)
            {
              await ConnectToBusBAsync(pointPair, messageService);
            }
          }
          else
          {
            await ConnectToBusBAsync(point.point, messageService);
          }
        }

      }
    }


    /// <summary>
    /// Отключает все точки группы к соответствующей шине в зависимости от текущего разряда.
    /// </summary>
    static private async Task DisconnectPointsToBusAsync(List<(PointModel point, string reversedBinary)> points, SchemeModel schemeModel, int step, IUserMessageService messageService)
    {
      foreach (var point in points)
      {
        if (point.reversedBinary[step] == '1')
        {
          if (schemeModel.TryCommunicatedPointAllChain(point.point, out List<PointModel> result))
          {
            if (point.point.PointNumber != result[0].PointNumber)
            {
              return;
            }

            foreach (var pointPair in result)
            {
              await DisconnectFromBusAAsync(pointPair, messageService);
            }
          }
          else
          {
            await DisconnectFromBusAAsync(point.point, messageService);
          }
        }
        else
        {

          if (schemeModel.TryCommunicatedPointAllChain(point.point, out List<PointModel> result))
          {
            if (point.point.PointNumber != result[0].PointNumber)
            {
              return;
            }

            foreach (var pointPair in result)
            {
              await DisconnectFromBusBAsync(pointPair, messageService);
            }
          }
          else
          {
            await DisconnectFromBusBAsync(point.point, messageService);
          }
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
  }
}
