using Ask.Core.Services.App;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Device.Communication.Ethernet.Udp;
using Ask.Device.Runtime.Ethernet.Udp.Broadcast;
using Ask.Engine.Tests.Base;
using DataBaseConfiguration.Services.Device;
using static Ask.Engine.Tests.Base.UIValidationHelper;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ask.Engine.Tests.NodeMethod
{
  /// <summary>
  /// Абстрактный класс для выполнения тестов методом узла.
  /// </summary>
  public abstract class BaseNodeTest
  {
    /// <summary>
    /// Начальная точка диапазона.
    /// </summary>
    public PointModel StartPoint { get; set; }

    /// <summary>
    /// Конечная точка диапазона.
    /// </summary>
    public PointModel EndPoint { get; set; }

    /// <summary>
    /// Список устройств, задействованных в тесте.
    /// </summary>
    protected List<object> Devices { get; } = new();

    /// <summary>
    /// Заданная шина.
    /// </summary>
    protected BusPoint AssignedBus { get; private set; }

    /// <summary>
    /// Противоположная шина.
    /// </summary>
    protected BusPoint OppositeBus { get; private set; }

    private List<PointModel> _pointsToProcess = new();
    private int _currentPointIndex = 0;

    /// <summary>
    /// Собирает устройства для выполнения теста.
    /// </summary>
    /// <param name="startPoint">Начальная точка диапазона.</param>
    /// <param name="endPoint">Конечная точка диапазона.</param>
    public virtual async Task CollectDevicesAsync(PointModel startPoint, PointModel endPoint)
    {
      Devices.Clear();

      var relayModules = RelayModuleHelper.GetModulesByRangeAsync(startPoint.DeviceNumber, startPoint.ModuleNumber, endPoint.ModuleNumber).GetAwaiter().GetResult();
      foreach (var module in relayModules)
      {
        Devices.Add(module);
      }

      var uksh = (await SwitchingDevices.GetDevicesByNumberChassisAsync(startPoint.DeviceNumber)).FirstOrDefault();
      Devices.Add(uksh);

      var breakdown = BreakdownTesters.GetDevicesByNumberChassisAsync(startPoint.DeviceNumber).GetAwaiter().GetResult().FirstOrDefault();
      Devices.Add(breakdown);
    }

    /// <summary>
    /// Настраивает коммутацию перед измерением.
    /// </summary>
    /// <param name="protocolUI">Элемент вывода сообщений.</param>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    /// <param name="bus">Подлючаемая шина.</param>
    public virtual async Task SetupCommutation(IUserInteractionService protocolUI, PointModel point1, PointModel point2, BusPoint bus)
    {
      BusPoint oppositeBus = bus == BusPoint.A ? BusPoint.B : BusPoint.A;
      AssignedBus = bus;
      OppositeBus = oppositeBus;

      var relayModules = Devices.OfType<IRelaySwitchModule>().ToList();
      var busSwitcher = Devices.OfType<ISwitchingDevice>().FirstOrDefault();
      var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();

      await busSwitcher.ConnectorManager.ConnectBreakdownTester(protocolUI);

      foreach (var module in relayModules)
      {
        await protocolUI.ShowMessageAsync(new ShowMessageModel($"{module.Name}({module.Number})", message: $"Подключение к шинам A1B1", type: ShowMessageModel.MessageType.Info));
        await module.BusManager.ConnectBusAsync(SwitchingBus.A1, userMessageService: protocolUI);
        await module.BusManager.ConnectBusAsync(SwitchingBus.B1, userMessageService: protocolUI);

        int startPoint;
        int endPoint;

        if (module.Number == point1.ModuleNumber && module.Number == point2.ModuleNumber)
        {
          startPoint = point1.PointNumber;
          endPoint = point2.PointNumber;
        }
        else if (module.Number == point1.ModuleNumber)
        {
          startPoint = point1.PointNumber;
          endPoint = module.PointCount;
        }
        else if (module.Number == point2.ModuleNumber)
        {
          startPoint = 1;
          endPoint = point2.PointNumber;
        }
        else
        {
          startPoint = 1;
          endPoint = module.PointCount;
        }

        await module.PointManager.ConnectRelayGroupAsync(oppositeBus, startPoint, endPoint, protocolUI);

        await protocolUI.ShowMessageAsync(new ShowMessageModel($"{module.NumberChassis}.{module.Number}.{startPoint} - {endPoint}", message: $"Подключение точек к шинам", type: ShowMessageModel.MessageType.Info));
        for (int i = startPoint; i <= endPoint; i++)
        {
          _pointsToProcess.Add(new PointModel { DeviceNumber = module.NumberChassis, ModuleNumber = module.Number, PointNumber = i });
        }
      }
    }

    /// <summary>
    /// Выполняет действия для одной точки, включая отключение предыдущей.
    /// </summary>
    /// <param name="newPoint">Новая точка, которая будет обработана.</param>
    /// <param name="oldPoint">Старая точка, которая будет отключена. Может быть null.</param>
    /// <param name="protocolUI">Элемент вывода сообщений.</param>
    protected virtual async Task ProcessPointAsync(PointModel newPoint, PointModel oldPoint, IUserInteractionService protocolUI)
    {
      var relayModules = Devices.OfType<IRelaySwitchModule>().ToList();

      if (oldPoint != null)
      {
        var moduleForOldPoint = relayModules.FirstOrDefault(module => module.NumberChassis == oldPoint.DeviceNumber && module.Number == oldPoint.ModuleNumber);

        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          return await moduleForOldPoint.PointManager.ConnectingPointToNewBus(OppositeBus, oldPoint.PointNumber, protocolUI);
        }, protocolUI);
      }

      if (newPoint != null)
      {
        var moduleForNewPoint = relayModules.FirstOrDefault(module => module.NumberChassis == newPoint.DeviceNumber && module.Number == newPoint.ModuleNumber);
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          return await moduleForNewPoint.PointManager.ConnectingPointToNewBus(AssignedBus, newPoint.PointNumber, protocolUI);
        }, protocolUI);
      }
    }

    /// <summary>
    /// Переходит к следующей точке в диапазоне и вызывает процесс обработки точки.
    /// </summary>
    /// <param name="protocolUI">Элемент вывода сообщений.</param>
    /// <returns>True, если есть следующая точка; иначе — false.</returns>
    protected virtual async Task<(bool Step, PointModel PointModel)> GetNextPoint(IUserInteractionService protocolUI)
    {
      PointModel oldPoint = _currentPointIndex > 0 ? _pointsToProcess[_currentPointIndex - 1] : null;

      if (_currentPointIndex >= _pointsToProcess.Count)
      {
        await ProcessPointAsync(null, oldPoint, protocolUI);
        return (false, null);
      }

      var newPoint = _pointsToProcess[_currentPointIndex++];
      await ProcessPointAsync(newPoint, oldPoint, protocolUI);
      return (true, newPoint);
    }

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    /// <param name="protocolUI">Пользовательский элемент для вывода в протокол.</param>
    /// <param name="dataModel">Модель данных.</param>
    public abstract Task PerformMeasurement(IUserInteractionService protocolUI, DataModel dataModel);

    /// <summary>
    /// Завершает тест, выполняя очистку и отключение оборудования.
    /// </summary>
    public virtual async Task FinalizeAsync(IUserInteractionService messageService)
    {
      await UdpBroadcastCommandSender.ResetAllDevicesAsync();
    }

    /// <summary>
    /// Проверяет и подключает все необходимые устройства перед выполнением теста.
    /// </summary>
    /// <returns>Задача, представляющая операцию подключения.</returns>
    public virtual async Task<(bool Connect, string Message)> ConnectDevicesAsync(IUserInteractionService messageService)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Инициализация оборудования", type: ShowMessageModel.MessageType.Info));

      foreach (var device in Devices)
      {
        if (device is IDevice connectableDevice)
        {
          var (connected, message) = await connectableDevice.ConnectableManager.ConnectAsync(messageService);
          if (!connected)
          {
            return (false, $"Не удалось подключить устройство {connectableDevice.Name}({connectableDevice.Number}) - {message} ");
          }

          await connectableDevice.ConnectableManager.ResetAsync(messageService);
        }
      }

      return (true, string.Empty);
    }

    /// <summary>
    /// Настраивает измерительное устройство (мультиметр или ППУ).
    /// </summary>
    /// <param name="dataModel">Модель данных, содержащая дополнительные значения для устройств.</param>
    public abstract Task ConfigureMeter(IUserInteractionService messageService, DataModel dataModel = null);

    public void ResetPoints()
    {
      _pointsToProcess.Clear();
      _currentPointIndex = 0;
    }

    /// <summary>
    /// Подключает все устройства, собранные в коллекции <see cref="Devices"/>.
    /// Если устройство уже подключено, повторное подключение не выполняется.
    /// </summary>
    /// <param name="point1">Первая точка (в формате A.B.C).</param>
    /// <param name="point2">Вторая точка (в формате A.B.C).</param>
    /// <param name="protocolUI">Пользовательский элемент для вывода в протокол.</param>
    public virtual async Task<(bool Connect, string Message)> ConnectToEquipment(PointModel point1, PointModel point2, IUserInteractionService protocolUI)
    {
      try
      {
        CollectDevicesAsync(point1, point2);
      }
      catch (Exception ex)
      {
        return (false, ex.Message);
      }

      var connect = await ConnectDevicesAsync(protocolUI);

      if (connect.Connect)
      {
        return (true, string.Empty);
      }
      else
      {
        return (false, connect.Message);
      }
    }
  }
}
