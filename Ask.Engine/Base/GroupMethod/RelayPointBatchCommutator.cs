using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.Base.GroupMethod
{
  /// <summary>
  /// Выполняет групповую коммутацию точек релейных модулей,
  /// автоматически объединяя последовательные точки в диапазоны
  /// для сокращения количества команд управления.
  /// </summary>
  internal static class RelayPointBatchCommutator
  {
    /// <summary>
    /// Подключает указанные точки к заданной шине.
    /// </summary>
    /// <param name="points">Коллекция точек для подключения.</param>
    /// <param name="bus">Шина, к которой необходимо подключить точки.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Асинхронная задача выполнения операции подключения.</returns>
    public static Task ConnectPointsAsync(IEnumerable<PointModel> points, BusPoint bus, IUserInteractionService messageService)
    {
      return ExecuteAsync(points, bus, messageService, isConnect: true);
    }

    /// <summary>
    /// Подключает указанные точки к заданной шине с использованием
    /// указанного релейного коммутатора.
    /// </summary>
    /// <param name="module">Релейный коммутатор.</param>
    /// <param name="points">Коллекция точек для подключения.</param>
    /// <param name="bus">Шина, к которой необходимо подключить точки.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Асинхронная задача выполнения операции подключения.</returns>
    public static Task ConnectPointsAsync(IRelaySwitchModule module, IEnumerable<PointModel> points, BusPoint bus, IUserInteractionService messageService)
    {
      var pointNumbers = points
        .Where(point => point != null)
        .Select(point => point.PointNumber);

      return ExecuteAsync(module, pointNumbers, bus, messageService, isConnect: true);
    }

    /// <summary>
    /// Отключает указанные точки от заданной шины.
    /// </summary>
    /// <param name="points">Коллекция точек для отключения.</param>
    /// <param name="bus">Шина, от которой необходимо отключить точки.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Асинхронная задача выполнения операции отключения.</returns>
    public static Task DisconnectPointsAsync(IEnumerable<PointModel> points, BusPoint bus, IUserInteractionService messageService)
    {
      return ExecuteAsync(points, bus, messageService, isConnect: false);
    }

    /// <summary>
    /// Отключает указанные точки от заданной шины с использованием
    /// указанного релейного коммутатора.
    /// </summary>
    /// <param name="module">Релейный коммутатор.</param>
    /// <param name="points">Коллекция точек для отключения.</param>
    /// <param name="bus">Шина, от которой необходимо отключить точки.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Асинхронная задача выполнения операции отключения.</returns>
    public static Task DisconnectPointsAsync(IRelaySwitchModule module, IEnumerable<PointModel> points, BusPoint bus, IUserInteractionService messageService)
    {
      var pointNumbers = points
        .Where(point => point != null)
        .Select(point => point.PointNumber);

      return ExecuteAsync(module, pointNumbers, bus, messageService, isConnect: false);
    }

    /// <summary>
    /// Выполняет подключение или отключение точек, сгруппированных
    /// по релейным коммутаторам.
    /// </summary>
    /// <param name="points">Коллекция точек, над которыми выполняется операция.</param>
    /// <param name="bus">Шина, с которой выполняется подключение или отключение.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="isConnect">
    /// <see langword="true"/> — подключить точки;
    /// <see langword="false"/> — отключить точки.
    /// </param>
    /// <returns>Асинхронная задача выполнения операции.</returns>
    private static async Task ExecuteAsync(IEnumerable<PointModel> points, BusPoint bus, IUserInteractionService messageService, bool isConnect)
    {
      var moduleGroups = points
        .Where(point => point != null)
        .GroupBy(point => (point.DeviceNumber, point.ModuleNumber));

      foreach (var moduleGroup in moduleGroups)
      {
        var module = GetModuleOrThrow(moduleGroup.First());
        await ExecuteAsync(module, moduleGroup.Select(point => point.PointNumber), bus, messageService, isConnect);
      }
    }

    /// <summary>
    /// Выполняет подключение или отключение набора точек
    /// на указанном релейном коммутаторе.
    /// </summary>
    /// <param name="module">Релейный коммутатор.</param>
    /// <param name="pointNumbers">Номера точек, над которыми выполняется операция.</param>
    /// <param name="bus">Шина, с которой выполняется подключение или отключение.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="isConnect">
    /// <see langword="true"/> — подключить точки;
    /// <see langword="false"/> — отключить точки.
    /// </param>
    /// <returns>Асинхронная задача выполнения операции.</returns>
    private static async Task ExecuteAsync(IRelaySwitchModule module, IEnumerable<int> pointNumbers, BusPoint bus, IUserInteractionService messageService, bool isConnect)
    {
      var orderedPointNumbers = pointNumbers
        .Distinct()
        .OrderBy(pointNumber => pointNumber);

      foreach (var range in BuildContinuousRanges(orderedPointNumbers))
      {
        if (range.First == range.Last)
        {
          if (isConnect)
          {
            await module.PointManager.ConnectRelayAsync(bus, range.First, messageService);
          }
          else
          {
            await module.PointManager.DisconnectRelayAsync(bus, range.First, messageService);
          }
        }
        else
        {
          if (isConnect)
          {
            await module.PointManager.ConnectRelayGroupAsync(bus, range.First, range.Last, messageService);
          }
          else
          {
            await module.PointManager.DisconnectRelayGroupAsync(bus, range.First, range.Last, messageService);
          }
        }
      }
    }

    /// <summary>
    /// Формирует последовательность непрерывных диапазонов номеров точек.
    /// </summary>
    /// <param name="pointNumbers">
    /// Последовательность номеров точек, отсортированная по возрастанию.
    /// </param>
    /// <returns>
    /// Последовательность диапазонов непрерывных номеров точек.
    /// </returns>
    private static IEnumerable<PointRange> BuildContinuousRanges(IEnumerable<int> pointNumbers)
    {
      using var enumerator = pointNumbers.GetEnumerator();
      if (!enumerator.MoveNext())
      {
        yield break;
      }

      var first = enumerator.Current;
      var last = first;

      while (enumerator.MoveNext())
      {
        var current = enumerator.Current;
        if (current == last + 1)
        {
          last = current;
          continue;
        }

        yield return new PointRange(first, last);
        first = current;
        last = current;
      }

      yield return new PointRange(first, last);
    }

    /// <summary>
    /// Возвращает релейный коммутатор, соответствующий указанной точке.
    /// </summary>
    /// <param name="point">Точка, для которой требуется определить релейный коммутатор.</param>
    /// <returns>Экземпляр релейного коммутатора.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если для указанной точки не найден релейный коммутатор.
    /// </exception>
    private static IRelaySwitchModule GetModuleOrThrow(PointModel point)
    {
      return EquipmentService.GetModuleByPoint(point)
        ?? throw new InvalidOperationException(
          $"Не удалось найти релейный коммутатор для точки [{point.DeviceNumber}.{point.ModuleNumber}]. " +
          "Убедитесь, что команда РМ была выполнена и параметры точки указаны корректно.");
    }

    /// <summary>
    /// Представляет непрерывный диапазон номеров точек.
    /// </summary>
    /// <param name="First">Первый номер точки в диапазоне.</param>
    /// <param name="Last">Последний номер точки в диапазоне.</param>
    private readonly record struct PointRange(int First, int Last);
  }
}
