using DataBaseConfiguration.Services;
using DataBaseConfiguration.Services.Device;
using Mode.Base;
using Mode.Models;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using UI.Controls.Protocol;
using Utilities.Models;
using static NewCore.Enum.MetrologyEnum;
using static Utilities.LoggerUtility;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Базовый класс для всех типов измерений, содержащий общий алгоритм.
  /// Использует шаблонный метод для автоматизации процесса.
  /// </summary>
  public abstract class BaseMeasurement
  {
    enum MetrologicalDeviceType
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
    public Dictionary<MetrologicalModeRole, List<object>> Devices { get; set; } = new();

    /// <summary>
    /// Формирует список уникальных устройств, необходимых для выполнения алгоритма,
    /// на основе заданных точек и выбранного метрологического режима.
    /// Устройства добавляются в коллекцию <see cref="Devices"/> по их логической роли.
    /// </summary>
    /// <param name="point1">Первая точка (в формате A.B.C).</param>
    /// <param name="point2">Вторая точка (в формате A.B.C).</param>
    /// <param name="mode">Метрологический режим, для которого выполняется алгоритм.</param>
    protected virtual void CollectDevices(PointModel point1, PointModel point2, MetrologicalModeRole mode)
    {
      Devices.Clear();

      if (point1 == null || point2 == null)
      {
        throw new ArgumentException("Одна или обе точки не удалось разобрать.");
      }

      var relayRepo = new RelaySwitchModuleServices();
      var ukshRepo = new SwitchingDeviceServices();

      var mkr1 = relayRepo.GetDevicesByNumberChassis(point1.DeviceNumber)
       .FirstOrDefault(m => m.Number == point1.ModuleNumber);
      AddUniqueDevice(mode, mkr1);

      if (point1.DeviceNumber != point2.DeviceNumber || point1.ModuleNumber != point2.ModuleNumber)
      {
        var mkr2 = relayRepo.GetDevicesByNumberChassis(point2.DeviceNumber)
            .FirstOrDefault(m => m.Number == point2.ModuleNumber);
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
          throw new ArgumentException("Мультиметр не найден в конфигурации!");
        }

        AddUniqueDevice(mode, fast);

        if (modeDevice == MetrologicalDeviceType.Mint)
        {
          var power = new PowerSourceModuleServices();
          var mint = power.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
          if (fast == null)
          {
            throw new ArgumentException("Мультиметр не найден в конфигурации!");
          }

          AddUniqueDevice(mode, mint);
        }
      }
      else if (modeDevice == MetrologicalDeviceType.BreakdownTester)
      {
        var breakdownRepo = new BreakdownTesterServices();
        var breakdown = breakdownRepo.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
        AddUniqueDevice(mode, breakdown);
      }
      else
      {
        throw new ArgumentException($"Метрологический режим {mode} не распознан.");
      }
    }

    /// <summary>
    /// Подключает все устройства, собранные в коллекции <see cref="Devices"/>.
    /// Если устройство уже подключено, повторное подключение не выполняется.
    /// </summary>
    /// <param name="point1">Первая точка (в формате A.B.C).</param>
    /// <param name="point2">Вторая точка (в формате A.B.C).</param>
    /// <param name="mode">Метрологический режим, для которого выполняется алгоритм.</param>
    /// <param name="protocolUI">Пользовательский элемент для вывода в протокол.</param>
    public virtual async Task<(bool Connect, string Message)> ConnectToEquipment(PointModel point1, PointModel point2, MetrologicalModeRole mode, ProtocolUI protocolUI)
    {
      try
      {
        CollectDevices(point1, point2, mode);
      }
      catch (Exception ex)
      {
        return (false, ex.Message);
      }

      await protocolUI.ShowMessageAsync(new ShowMessageModel("Инициализация устройств", ShowMessageModel.SuccessMessage.TitleColor));

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
              var (connected, message) = await connectableDevice.ConnectableManager.ConnectAsync();
              if (!connected)
              {
                return (false, $"Не удалось подключить устройство {connectableDevice.Name}({connectableDevice.Number}) - {message} ");

                throw new InvalidOperationException($"Не удалось подключить устройство с ролью {role}: {message}");
              }

              LogInformation($"Устройство с ролью {role} успешно подключено.");
            }
            else
            {
              LogWarning($"Устройство с ролью {role} не поддерживает подключение.");
            }
          }
        }

        return (true, string.Empty);
      }
      catch (Exception ex)
      {
        return (false, ex.Message);
      }
    }

    /// <summary>
    /// Настраивает коммутацию перед измерением.
    /// </summary>
    /// <param name="protocolUI">Элемент управления для вывода данных.</param>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    /// <param name="mode">Режим метрологии.</param>
    public virtual async Task SetupCommutation(ProtocolUI protocolUI, PointModel point1, PointModel point2, MetrologicalModeRole mode)
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
    public virtual async Task ConfigureMeter(MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
    { 
      await AppConfiguration.Services.UserMessageServiceProvider.ShowMessageAsync(new ShowMessageModel("Настройка измерителя", ShowMessageModel.SuccessMessage.TitleColor));
    }

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    /// <param name="metrologicalModeRole">Метрологический режим.</param>
    /// <param name="param">Электрическое значение.</param>
    /// <param name="protocolUI">Пользовательский элемент для вывода в протокол.</param>
    public abstract Task PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI);

    /// <summary>
    /// Завершает измерение, размыкает реле и отключает прибор.
    /// </summary>
    public virtual async Task FinalizeMeasurement()
    {
      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }
    }

    /// <summary>
    /// Добавляет устройство в коллекцию по роли, если оно ещё не добавлено.
    /// </summary>
    /// <param name="role">Логическая роль устройства.</param>
    /// <param name="device">Экземпляр устройства для добавления.</param>
    protected void AddUniqueDevice(MetrologicalModeRole role, object device)
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
    protected T GetDevice<T>(MetrologicalModeRole role, int index = 0) where T : class
    {
      if (Devices.TryGetValue(role, out var list) &&
          list.Count > index &&
          list[index] is T typed)
      {
        return typed;
      }

      throw new InvalidOperationException($"Устройство с ролью {role} (index: {index}) не найдено или не реализует интерфейс {typeof(T).Name}.");
    }

    private static MetrologicalDeviceType GetDeviceTypeForMode(MetrologicalModeRole mode)
    {
      switch (mode)
      {
        case MetrologicalModeRole.IE:
        case MetrologicalModeRole.KC:
          return MetrologicalDeviceType.FastMeter;
        case MetrologicalModeRole.PR:
          return MetrologicalDeviceType.Mint;

        case MetrologicalModeRole.CI:
        case MetrologicalModeRole.PI:
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
    public List<IRelaySwitchModule>? GetRelayModules(MetrologicalModeRole mode)
    {
      return Devices.TryGetValue(mode, out var modules)
          ? modules.OfType<IRelaySwitchModule>().ToList()
          : null;
    }

    /// <summary>
    /// Возвращает устройство коммутации шин (УКШ), связанное с заданным режимом.
    /// </summary>
    /// <param name="mode">Метрологическая роль устройства.</param>
    /// <returns>Устройство коммутации шин или null, если не найдено.</returns>
    public ISwitchingDevice? GetBusSwitcher(MetrologicalModeRole mode)
    {
      return Devices.TryGetValue(mode, out var ukshs)
          ? ukshs.OfType<ISwitchingDevice>().FirstOrDefault()
          : null;
    }

    /// <summary>
    /// Возвращает модуль источника напряжения и тока (МИНТ), связанный с заданным режимом.
    /// </summary>
    /// <param name="mode">Метрологическая роль устройства.</param>
    /// <returns>Модуль МИНТ или null, если не найдено.</returns>
    public IPowerSourceModule? GetMintModule(MetrologicalModeRole mode)
    {
      return Devices.TryGetValue(mode, out var mints)
          ? mints.OfType<IPowerSourceModule>().FirstOrDefault()
          : null;
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
      {
        throw new InvalidOperationException("Не найдено ни одного модуля коммутации реле (МКР).");
      }

      if (busSwitcher == null)
      {
        throw new InvalidOperationException("Не найдено устройство коммутации шин (УКШ).");
      }

      if (modeDevice == MetrologicalDeviceType.Mint && mint == null)
      {
        throw new InvalidOperationException("Не найден модуль источника напряжения и тока (МИНТ).");
      }
    }

    /// <summary>
    /// Подключает соответствующие шины в зависимости от типа метрологического устройства.
    /// </summary>
    /// <param name="relayModules">Список модулей коммутации реле.</param>
    /// <param name="modeDevice">Тип метрологического устройства.</param>
    private async Task ConnectBusesAsync(ISwitchingDevice busSwitcher, IPowerSourceModule mint, List<IRelaySwitchModule> relayModules, MetrologicalDeviceType modeDevice, ProtocolUI protocolUI)
    {
      await protocolUI.ShowMessageAsync(new ShowMessageModel("Подключение шин", ShowMessageModel.SuccessMessage.TitleColor));

      foreach (var relayModule in relayModules)
      {
        await relayModule.BusManager.ConnectBusAsync(NewCore.Enum.DeviceEnum.SwitchingBus.A1);
        await relayModule.BusManager.ConnectBusAsync(NewCore.Enum.DeviceEnum.SwitchingBus.B1);
      }

      if (modeDevice == MetrologicalDeviceType.BreakdownTester)
      {
        await busSwitcher.ConnectorManager.ConnectBreakdownTester();
      }
      else
      {
        if (modeDevice == MetrologicalDeviceType.Mint)
        {
          await mint.BusManager.ConnectBusToPositiveAsync(NewCore.Enum.DeviceEnum.SwitchingBus.A1);
          await mint.BusManager.ConnectBusToNegativeAsync(NewCore.Enum.DeviceEnum.SwitchingBus.B1);
        }
        await busSwitcher.ConnectorManager.ConnectMultimeter(NewCore.Enum.DeviceEnum.SwitchingBusNew.AB1);
      }
    }

    /// <summary>
    /// Подключает реле к заданным точкам коммутации.
    /// </summary>
    /// <param name="relayModules">Список модулей коммутации реле.</param>
    /// <param name="point1">Первая точка коммутации.</param>
    /// <param name="point2">Вторая точка коммутации.</param>
    private async Task ConnectRelayPointsAsync(List<IRelaySwitchModule> relayModules, PointModel point1, PointModel point2, ProtocolUI protocolUI)
    {
      await protocolUI.ShowMessageAsync(new ShowMessageModel("Подключение точек", ShowMessageModel.SuccessMessage.TitleColor));

      await relayModules[0].PointManager.ConnectRelayAsync(NewCore.Enum.DeviceEnum.BusPoint.A, point1.PointNumber);
      await relayModules.Last().PointManager.ConnectRelayAsync(NewCore.Enum.DeviceEnum.BusPoint.B, point2.PointNumber);
    }
    #endregion
  }
}
