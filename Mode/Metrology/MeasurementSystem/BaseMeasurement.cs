using System.Globalization;
using System.Text.RegularExpressions;
using AppConfig.DataBase.Repositories;
using AppConfig.DataBase.Services;
using Mode.Models;
using NewCore.Base.Device;
using UI.Components;
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
    #region Репозитории устройств.
    /// <summary>
    /// Коллекция подключённых устройств, сгруппированных по метрологическим ролям.
    /// Каждая роль может иметь одно или несколько устройств (например, два модуля коммутации для КС).
    /// </summary>
    public Dictionary<MetrologicalModeRole, List<object>> Devices { get; set; } = new();

    /// <summary>
    /// Репозиторий для работы с менеджерами шасси.
    /// Используется для проверки существования шасси в базе данных.
    /// </summary>
    private readonly ChassisManagerServices _chassisManagerRepository = new ChassisManagerServices();

    /// <summary>
    /// Репозиторий для работы с модулями коммутации реле.
    /// Используется для проверки существования модуля и точки.
    /// </summary>
    private readonly RelaySwitchModuleServices _relaySwitchModuleRepository = new RelaySwitchModuleServices();

    #endregion

    /// <summary>
    /// Запускает процесс измерения.
    /// </summary>
    /// <param name="mode">Метрологический режим.</param>
    /// <param name="point1">Первая точка измерения.</param>
    /// <param name="point2">Вторая точка измерения.</param>
    /// <param name="referenceValue">Эталонное значение.</param>
    public void ExecuteMeasurement(MetrologicalModeRole mode, PointModel point1, PointModel point2, double referenceValue)
    {
      // CollectDevices(point1, point2, mode);
      // ConnectToEquipment();
      // SetupCommutation(point1, point2);
      // ConfigureMultimeter();
      // PerformMeasurement();
      // FinalizeMeasurement();
    }

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
      AddUniqueDevice(MetrologicalModeRole.KC, mkr1);

      if (point1.DeviceNumber != point2.DeviceNumber || point1.ModuleNumber != point2.ModuleNumber)
      {
        var mkr2 = relayRepo.GetDevicesByNumberChassis(point2.DeviceNumber)
            .FirstOrDefault(m => m.Number == point2.ModuleNumber);
        AddUniqueDevice(MetrologicalModeRole.KC, mkr2);
      }

      var uksh = ukshRepo.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
      AddUniqueDevice(MetrologicalModeRole.KC, uksh);

      switch (mode)
      {
        case MetrologicalModeRole.PR:
        case MetrologicalModeRole.IE:
        case MetrologicalModeRole.KC:
          {
            var fastRepo = new FastMeterServices();

            var fast = fastRepo.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
            if (fast == null)
            {
              throw new ArgumentException("Мультиметр не найден в конфигурации!");
            }

            AddUniqueDevice(MetrologicalModeRole.PR, fast);
            break;
          }

        case MetrologicalModeRole.CI:
          {
            var breakdownRepo = new BreakdownTesterServices();
            var breakdown = breakdownRepo.GetDevicesByNumberChassis(point1.DeviceNumber).FirstOrDefault();
            AddUniqueDevice(MetrologicalModeRole.IE, breakdown);
            break;
          }
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

            // TODO : Заглушка
            //if (await AppConfig.Config.ProtocolConfig.GetDeviceInfo())
            if (true)
            {
              await protocolUI.ShowMessageAsync(new ShowMessageModel($"{connectableDevice.Name}({connectableDevice.Number})", message: $"[{ShowMessageModel.SuccessMessage.Item1}]", messageColor: ShowMessageModel.SuccessMessage.Item2));
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

    /// <summary>
    /// Настраивает коммутацию перед измерением.
    /// </summary>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    protected virtual void SetupCommutation(PointModel point1, PointModel point2)
    {
      // TODO: Реализовать настройку коммутации
    }

    /// <summary>
    /// Настраивает измерительное устройство (мультиметр или ППУ).
    /// </summary>
    /// <param name="device">Объект настройки.</param>
    protected abstract void ConfigureMultimeter();

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    protected virtual void PerformMeasurement()
    {
      // TODO: Реализовать процесс измерения
    }

    /// <summary>
    /// Завершает измерение, размыкает реле и отключает прибор.
    /// </summary>
    protected virtual void FinalizeMeasurement()
    {
      // TODO: Реализовать завершение измерения
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

    #region private

    /// <summary>
    /// Извлекает номер шасси из точки A.B.C.
    /// </summary>
    /// <param name="point">Точка в формате A.B.C.</param>
    /// <returns>Номер шасси.</returns>
    private int GetChassisNumber(string point)
    {
      return int.Parse(point.Split('.')[0]);
    }

    /// <summary>
    /// Извлекает номер модуля коммутации реле из точки A.B.C.
    /// </summary>
    /// <param name="point">Точка в формате A.B.C.</param>
    /// <returns>Номер модуля.</returns>
    private int GetModuleNumber(string point)
    {
      return int.Parse(point.Split('.')[1]);
    }

    /// <summary>
    /// Извлекает номер точки из точки A.B.C.
    /// </summary>
    /// <param name="point">Точка в формате A.B.C.</param>
    /// <returns>Номер точки.</returns>
    private int GetPointNumber(string point)
    {
      return int.Parse(point.Split('.')[2]);
    }

    /// <summary>
    /// Проверяет, существует ли указанное шасси в базе данных.
    /// </summary>
    /// <param name="chassisNumber">Номер шасси.</param>
    /// <returns>True, если шасси существует, иначе false.</returns>
    private bool ChassisExistsInDatabase(int chassisNumber)
    {
      return _chassisManagerRepository.GetByNumber(chassisNumber) != null;
    }

    /// <summary>
    /// Проверяет, можно ли преобразовать строку в корректное положительное число.
    /// </summary>
    /// <param name="value">Строковое значение параметра.</param>
    /// <param name="parsedValue">Выходной параметр с преобразованным значением.</param>
    /// <returns>True, если преобразование успешно и число положительное, иначе false.</returns>
    private bool IsValidElectricalParameter(string value, out double parsedValue)
    {
      if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
      {
        return !double.IsNaN(parsedValue) && !double.IsInfinity(parsedValue) && parsedValue >= 0;
      }

      return false;
    }

    private void RaisePointValidationEvent(string label)
    {
      if (label == "Первая")
      {
        Utilities.Events.InputValidationEvents.TriggerInvalidFirstPoint = true;
      }
      else if (label == "Вторая")
      {
        Utilities.Events.InputValidationEvents.TriggerInvalidSecondPoint = true;
      }
    }

    #endregion
  }
}
