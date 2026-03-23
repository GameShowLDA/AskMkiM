using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.Errors.Metrology;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Device.Communication.Ethernet.Udp;
using Ask.Device.Communication.Ethernet.Udp.Broadcast;
using DataBaseConfiguration.Services.Device;
using static Ask.Engine.Tests.Base.UIValidationHelper;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.Tests.Metrology.MeasurementSystem
{
  /// <summary>
  /// Базовый класс для всех типов измерений, содержащий общий алгоритм.
  /// Использует шаблонный метод для автоматизации процесса.
  /// </summary>
  public abstract class BaseMeasurement
  {
    private enum MetrologicalDeviceType
    {
      FastMeter,
      BreakdownTester,
      Mint,
      None,
    }

    /// <summary>
    /// Коллекция подключённых устройств, сгруппированных по метрологическим ролям.
    /// Каждая роль может иметь одно или несколько устройств (например, два модуля коммутации для КС).
    /// </summary>
    public Dictionary<MeasurementTypeCommand, List<object>> Devices { get; set; } = new();
    public (PointModel points1, PointModel pointModel2) Points { get; set; } = new();

    internal List<double> Measurements { get; set; } = new();
    internal double LowerBound = -1;
    internal double UpperBound = -1;


    /// <summary>
    /// Формирует список уникальных устройств, необходимых для выполнения алгоритма,
    /// на основе заданных точек и выбранного метрологического режима.
    /// Устройства добавляются в коллекцию <see cref="Devices"/> по их логической роли.
    /// </summary>
    /// <param name="point1">Первая точка (в формате A.B.C).</param>
    /// <param name="point2">Вторая точка (в формате A.B.C).</param>
    /// <param name="mode">Метрологический режим, для которого выполняется алгоритм.</param>
    protected virtual void CollectDevices(PointModel point1, PointModel point2, MeasurementTypeCommand mode)
    {
      Devices.Clear();

      if (point1 == null || point2 == null)
      {
        throw MetrologyValidationErrors.PointParsingFailed();
      }

      var relayRepo = new RelaySwitchModuleServices();
      var ukshRepo = new SwitchingDeviceServices();

      var mkr1 = relayRepo.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault(m => m.Number == point1.ModuleNumber);
      AddUniqueDevice(mode, mkr1);

      if (point1.DeviceNumber != point2.DeviceNumber || point1.ModuleNumber != point2.ModuleNumber)
      {
        var mkr2 = relayRepo.GetDevicesByNumberChassis(point2.DeviceNumber).FirstOrDefault(m => m.Number == point2.ModuleNumber);
        AddUniqueDevice(mode, mkr2);
      }

      var uksh = ukshRepo.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
      AddUniqueDevice(mode, uksh);

      var modeDevice = GetDeviceTypeForMode(mode);
      if (modeDevice == MetrologicalDeviceType.FastMeter || modeDevice == MetrologicalDeviceType.Mint)
      {
        var fastRepo = new FastMeterServices();
        var fast = fastRepo.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
        if (fast == null)
        {
          throw ConnectionExceptionAdapter.NotFoundInConfiguration("Мультиметр");
        }

        AddUniqueDevice(mode, fast);

        if (modeDevice == MetrologicalDeviceType.Mint)
        {
          var power = new PowerSourceModuleServices();
          var mint = power.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
          AddUniqueDevice(mode, mint);
        }
      }
      else if (modeDevice == MetrologicalDeviceType.BreakdownTester)
      {
        var svc = ServiceLocator.GetRequired<BreakdownTesterServices>();
        var breakdown = svc.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
        AddUniqueDevice(mode, breakdown);
      }
      else
      {
        throw MetrologyValidationErrors.UnknownMetrologicalMode(mode.ToString());
      }

      Points = (point1, point2);
    }

    /// <summary>
    /// Подключает все устройства, собранные в коллекции <see cref="Devices"/>.
    /// Если устройство уже подключено, повторное подключение не выполняется.
    /// </summary>
    /// <param name="point1">Первая точка (в формате A.B.C).</param>
    /// <param name="point2">Вторая точка (в формате A.B.C).</param>
    /// <param name="mode">Метрологический режим, для которого выполняется алгоритм.</param>
    /// <param name="messageService">Пользовательский элемент для вывода в протокол.</param>
    public virtual async Task ConnectToEquipment(PointModel point1, PointModel point2, MeasurementTypeCommand mode, IUserInteractionService messageService)
    {
      try
      {
        CollectDevices(point1, point2, mode);
      }
      catch (Exception ex)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: ex.Message, type: ShowMessageModel.MessageType.Error));
        throw MetrologyValidationErrors.DeviceCollectFailed(ex);
      }

      if (DeviceDisplayConfig.GetExecutionParametersVisibility())
      {
        await messageService.ShowMessageAsync(new ShowMessageModel("Инициализация устройств", type: ShowMessageModel.MessageType.Info), IsBlockStart: true);
      }

      try
      {
        var connectedDevices = new HashSet<object>();

        foreach (var role in Devices.Keys)
        {
          foreach (var device in Devices[role])
          {
            if (connectedDevices.Contains(device))
            {
              continue;
            }

            connectedDevices.Add(device);

            if (device is IDevice connectableDevice)
            {
              var (connected, message) = await connectableDevice.ConnectableManager.ConnectAsync(messageService);
              if (!connected)
              {
                await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: $"Не удалось подключить устройство {connectableDevice.Name}({connectableDevice.Number}) - {message} ", type: ShowMessageModel.MessageType.Error));
                throw ConnectionExceptionAdapter.ConnectByRoleFailed(role.ToString());
              }

              LogInformation($"Устройство с ролью {role} успешно подключено.");
            }
            else
            {
              LogWarning($"Устройство с ролью {role} не поддерживает подключение.");
            }
          }
        }
      }
      catch (Exception ex)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: ex.Message, type: ShowMessageModel.MessageType.Error));
        throw SystemUnexpectedErrors.Unexpected(ex);
      }
    }

    /// <summary>
    /// Настраивает коммутацию перед измерением.
    /// </summary>
    /// <param name="protocolUI">Элемент управления для вывода данных.</param>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    /// <param name="mode">Режим метрологии.</param>
    public virtual async Task SetupCommutation(IUserInteractionService protocolUI, PointModel point1, PointModel point2, MeasurementTypeCommand mode)
    {
      var relayModules = GetRelayModules(mode);
      var busSwitcher = GetBusSwitcher(mode);
      var mint = GetMintModule(mode);
      var modeDevice = GetDeviceTypeForMode(mode);

      ValidateDevices(relayModules, busSwitcher, mint, modeDevice);

      await ConnectBusesAsync(busSwitcher, mint, relayModules, modeDevice, protocolUI);
      await ConnectRelayPointsAsync(relayModules, point1, point2, protocolUI);
    }

    /// <summary>
    /// Настраивает измерительное устройство (мультиметр или ППУ).
    /// </summary>
    /// <param name="metrologicalModeRole">Метрологический режим.</param>
    /// <param name="dataModel">Модель данных, содержащая дополнительные значения для устройств.</param>
    public virtual async Task ConfigureMeter(IUserInteractionService messageService, MeasurementTypeCommand metrologicalModeRole, DataModel dataModel = null)
    {
      if (DeviceDisplayConfig.GetExecutionParametersVisibility())
      {
        await messageService.ShowMessageAsync(new ShowMessageModel("Настройка измерителя", type: ShowMessageModel.MessageType.Info), IsBlockStart: true);
      }
    }

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    /// <param name="metrologicalModeRole">Метрологический режим.</param>
    /// <param name="param">Электрическое значение.</param>
    /// <param name="protocolUI">Пользовательский элемент для вывода в протокол.</param>
    public abstract Task<bool> PerformMeasurement(MeasurementTypeCommand metrologicalModeRole, double param, IUserInteractionService protocolUI, double intrinsicValue = 0);

    /// <summary>
    /// Завершает измерение, размыкает реле и отключает прибор.
    /// </summary>
    public virtual async Task FinalizeMeasurement(IUserInteractionService messageService)
    {
      await UdpBroadcastCommandSender.ResetAllDevicesAsync();
    }

    /// <summary>
    /// Добавляет устройство в коллекцию по роли, если оно ещё не добавлено.
    /// </summary>
    /// <param name="role">Логическая роль устройства.</param>
    /// <param name="device">Экземпляр устройства для добавления.</param>
    protected void AddUniqueDevice(MeasurementTypeCommand role, object device)
    {
      if (device == null)
      {
        return;
      }

      if (!Devices.ContainsKey(role))
      {
        Devices[role] = new List<object>();
      }

      if (!Devices[role].Contains(device))
      {
        Devices[role].Add(device);
      }
    }

    /// <summary>
    /// Получает устройство по заданной роли и индексу, с приведением к нужному типу.
    /// </summary>
    /// <typeparam name="T">Ожидаемый тип интерфейса устройства.</typeparam>
    /// <param name="role">Роль устройства в алгоритме.</param>
    /// <param name="index">Индекс устройства (если несколько устройств одной роли).</param>
    /// <returns>Устройство, приведённое к типу T.</returns>
    /// <exception cref="InvalidOperationException">Если устройство не найдено или не того типа.</exception>
    protected T GetDevice<T>(MeasurementTypeCommand role, int index = 0) where T : class
    {
      if (Devices.TryGetValue(role, out var list) &&
          list.Count > index &&
          list[index] is T typed)
      {
        return typed;
      }

      throw MetrologyValidationErrors.DeviceByRoleNotFound(role, index, typeof(T));
    }

    public virtual async Task PrintResult(IMessageOutputService messageService, MeasurementTypeCommand command)
    {
      if (Measurements.Count < 2)
        return;

      var min = Measurements.Min();
      var max = Measurements.Max();

      if (min > 0)
      {
        min = 0;
      }

      if (max < 0)
      {
        max = 0;
      }

      var info = command.GetDisplayInfo();
      string displayName = info?.DisplayName ?? command.ToString();
      string unit = info?.Unit ?? "";

      await messageService.ShowMessageAsync(new ShowMessageModel($"Результаты режима {displayName}"), skipPause: true);
      await messageService.ShowMessageAsync(new ShowMessageModel("Максимальная отрицательная погрешность", message: $"{min:F5} {unit}", type: ShowMessageModel.MessageType.Info) { IndentLevel = 1 }, skipPause: true);
      await messageService.ShowMessageAsync(new ShowMessageModel("Максимальная положительная погрешность", message: $"{max:F5} {unit}", type: ShowMessageModel.MessageType.Info) { IndentLevel = 1 }, skipPause: true);

      Measurements = new();
    }

    private static MetrologicalDeviceType GetDeviceTypeForMode(MeasurementTypeCommand mode)
    {
      switch (mode)
      {
        case MeasurementTypeCommand.IE:
        case MeasurementTypeCommand.KC:
        case MeasurementTypeCommand.KN_DCW:
        case MeasurementTypeCommand.KN_ACW:
        case MeasurementTypeCommand.EHT:
        case MeasurementTypeCommand.PR:
          return MetrologicalDeviceType.FastMeter;


        case MeasurementTypeCommand.SI:
        case MeasurementTypeCommand.PI_ACW:
        case MeasurementTypeCommand.PI_DCW:
          return MetrologicalDeviceType.BreakdownTester;

        default:
          return MetrologicalDeviceType.None;
      }
    }

    #region private

    /// <summary>
    /// Возвращает список модулей коммутации реле (МКР), связанных с заданным режимом.
    /// </summary>
    /// <param name="mode">Метрологическая роль устройства.</param>
    /// <returns>Список модулей коммутации реле или null, если не найдено.</returns>
    public List<IRelaySwitchModule>? GetRelayModules(MeasurementTypeCommand mode)
    {
      return Devices.TryGetValue(mode, out var modules) ? modules.OfType<IRelaySwitchModule>().ToList() : null;
    }

    /// <summary>
    /// Возвращает модуль коммутации реле (МКР) с наибольшим номером,
    /// связанный с заданным режимом.
    /// </summary>
    /// <param name="mode">Метрологическая роль устройства.</param>
    /// <returns>Модуль с максимальным номером или null, если не найден.</returns>
    public IRelaySwitchModule? GetRelayModuleWithMaxNumber(MeasurementTypeCommand mode)
    {
      if (!Devices.TryGetValue(mode, out var modules))
        return null;

      return modules
          .OfType<IRelaySwitchModule>()
          .MaxBy(x => x.Number);
    }

    /// <summary>
    /// Возвращает устройство коммутации шин (УКШ), связанное с заданным режимом.
    /// </summary>
    /// <param name="mode">Метрологическая роль устройства.</param>
    /// <returns>Устройство коммутации шин или null, если не найдено.</returns>
    public ISwitchingDevice? GetBusSwitcher(MeasurementTypeCommand mode)
    {
      return Devices.TryGetValue(mode, out var ukshs) ? ukshs.OfType<ISwitchingDevice>().FirstOrDefault() : null;
    }

    /// <summary>
    /// Возвращает модуль источника напряжения и тока (МИНТ), связанный с заданным режимом.
    /// </summary>
    /// <param name="mode">Метрологическая роль устройства.</param>
    /// <returns>Модуль МИНТ или null, если не найдено.</returns>
    public IPowerSourceModule? GetMintModule(MeasurementTypeCommand mode)
    {
      return Devices.TryGetValue(mode, out var mints) ? mints.OfType<IPowerSourceModule>().FirstOrDefault() : null;
    }

    /// <summary>
    /// Возвращает модуль источника напряжения и тока (МИНТ), связанный с заданным режимом.
    /// </summary>
    /// <param name="mode">Метрологическая роль устройства.</param>
    /// <returns>Модуль МИНТ или null, если не найдено.</returns>
    public (PointModel Point1, PointModel Point2) GetPoints()
    {
      return Points;
    }


    /// <summary>
    /// Выполняет проверку наличия всех необходимых устройств для коммутации.
    /// Генерирует исключения при отсутствии обязательных компонентов.
    /// </summary>
    /// <param name="relayModules">Список модулей коммутации реле.</param>
    /// <param name="busSwitcher">Устройство коммутации шин.</param>
    /// <param name="mint">Модуль источника напряжения и тока.</param>
    /// <param name="modeDevice">Тип метрологического устройства.</param>
    private void ValidateDevices(List<IRelaySwitchModule> relayModules, ISwitchingDevice busSwitcher, IPowerSourceModule mint, MetrologicalDeviceType modeDevice)
    {
      if (relayModules == null || relayModules.Count == 0)
        throw ConnectionExceptionAdapter.NotFoundInConfiguration("Модуль коммутации реле (МКР)");

      if (busSwitcher == null)
        throw ConnectionExceptionAdapter.NotFoundInConfiguration("Устройство коммутации шин (УКШ)");

      if (modeDevice == MetrologicalDeviceType.Mint && mint == null)
        throw ConnectionExceptionAdapter.NotFoundInConfiguration("Модуль источника напряжения и тока (МИНТ)");
    }

    /// <summary>
    /// Подключает соответствующие шины в зависимости от типа метрологического устройства.
    /// </summary>
    /// <param name="relayModules">Список модулей коммутации реле.</param>
    /// <param name="modeDevice">Тип метрологического устройства.</param>
    private async Task ConnectBusesAsync(ISwitchingDevice busSwitcher, IPowerSourceModule mint, List<IRelaySwitchModule> relayModules, MetrologicalDeviceType modeDevice, IUserInteractionService protocolUI)
    {
      if (DeviceDisplayConfig.GetConnectionInfoVisibility())
      {
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Подключение шин"), IsBlockStart: true);
      }

      foreach (var relayModule in relayModules)
      {
        await relayModule.BusManager.ConnectBusAsync(SwitchingBus.A1, userMessageService: protocolUI);
        await relayModule.BusManager.ConnectBusAsync(SwitchingBus.B1, userMessageService: protocolUI);
      }

      if (modeDevice == MetrologicalDeviceType.BreakdownTester)
      {
        await busSwitcher.ConnectorManager.ConnectBreakdownTester(protocolUI);
      }
      else
      {
        if (modeDevice == MetrologicalDeviceType.Mint)
        {
          await mint.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A1, userMessageService: protocolUI);
          await mint.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B1, userMessageService: protocolUI);
        }

        await busSwitcher.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, protocolUI);
      }
    }

    /// <summary>
    /// Подключает реле к заданным точкам коммутации.
    /// </summary>
    /// <param name="relayModules">Список модулей коммутации реле.</param>
    /// <param name="point1">Первая точка коммутации.</param>
    /// <param name="point2">Вторая точка коммутации.</param>
    public virtual async Task ConnectRelayPointsAsync(List<IRelaySwitchModule> relayModules, PointModel point1, PointModel point2, IUserInteractionService protocolUI)
    {
      if (DeviceDisplayConfig.GetConnectionInfoVisibility())
      {
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Подключение точек"), IsBlockStart: true);
      }

      await relayModules[0].PointManager.ConnectRelayAsync(BusPoint.A, point1.PointNumber, protocolUI);
      await relayModules.Last().PointManager.ConnectRelayAsync(BusPoint.B, point2.PointNumber, protocolUI);
    }
    #endregion
  }
}
