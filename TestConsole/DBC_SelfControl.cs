using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Initialization;
using Ask.DataBase.Engine.Static.Devices;
using System.Reflection;
using static Ask.LogLib.LoggerUtility;
using EngineSwitchingDevices = Ask.DataBase.Engine.Static.Devices.SwitchingDevices;

namespace TestConsole
{
  internal class DBC_SelfControl
  {
    private enum UkshDatabaseSource
    {
      OldDatabase,
      NewDatabase,
    }

    private static bool meterConnect = false;
    private static bool dbcConnect = false;
    private static UkshDatabaseSource _ukshDatabaseSource = UkshDatabaseSource.OldDatabase;
    internal static async Task RunAsync()
    {
      Console.WriteLine("=== Самоконтроль УКШ ===");
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\nВыберите действие:");
        Console.WriteLine($"Источник УКШ: {GetUkshDatabaseSourceName(_ukshDatabaseSource)}");
        Console.WriteLine("1. Тест блокировочных реле");
        Console.WriteLine("2. Тест мультиметра");
        Console.WriteLine("3. Тест АЦП");
        Console.WriteLine("4. Тест АЦП с переполюсовкой");
        Console.WriteLine("5. Тест ПИНТ");
        Console.WriteLine("6. Тест Шунт");
        Console.WriteLine("7. Тест ППУ");
        Console.WriteLine("8. Весь самоконтроль");
        Console.WriteLine("9. Выбрать источник УКШ");
        Console.WriteLine("0. Выход");

        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 9)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        meterConnect = false;
        dbcConnect = false;

        switch (choice)
        {
          case 1:
            await SelfCheckCircuitAsync(SwitchingDeviceTypeConnector.BlockingRelay);
            break;

          case 2:
            await SelfCheckCircuitAsync(SwitchingDeviceTypeConnector.Multimeter);
            break;

          case 3:
            await SelfCheckCircuitAsync(SwitchingDeviceTypeConnector.ADC);
            break;

          case 4:
            await SelfCheckCircuitAsync(SwitchingDeviceTypeConnector.ADCReversed);
            break;

          case 5:
            await SelfCheckCircuitAsync(SwitchingDeviceTypeConnector.PINT);
            break;

          case 6:
            await SelfCheckCircuitAsync(SwitchingDeviceTypeConnector.Shunt);
            break;

          case 7:
            await SelfCheckCircuitAsync(SwitchingDeviceTypeConnector.BreakdownTester);
            break;

          case 8:

            var device = GetDeviceInstance(SelectDeviceBusCommutation);
            var meter = GetDeviceInstance(SelectMeter);

            LogInformation("Начинаем полный самоконтроль всех цепей...");

            foreach (SwitchingDeviceTypeConnector testType in Enum.GetValues(typeof(SwitchingDeviceTypeConnector)))
            {
              await Task.Delay(20);
              LogInformation($"Запуск проверки: {testType}");
              bool result = await SelfCheckCircuitAsync(testType, device, meter);

              if (!result)
              {
                LogError($"Ошибка в самоконтроле {testType}!");
              }

              Console.WriteLine();
            }

            LogDebug("Полный самоконтроль завершен.");
            break;

          case 9:
            _ukshDatabaseSource = SelectUkshDatabaseSource();
            Console.WriteLine($"Источник УКШ переключен: {GetUkshDatabaseSourceName(_ukshDatabaseSource)}");
            break;

          case 0:
            return;

          default:
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            break;
        }
      }
    }

    /// <summary>
    /// Выполняет самоконтроль указанной цепи, включая проверку главных реле на каждой шине.
    /// </summary>
    /// <param name="testType">Тип цепи для проверки.</param>
    /// <returns>True, если проверка успешна, иначе false.</returns>
    private static async Task<bool> SelfCheckCircuitAsync(SwitchingDeviceTypeConnector testType, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      if (device == null)
      {
        device = GetDeviceInstance(SelectDeviceBusCommutation);
      }

      if (meter == null)
      {
        meter = GetDeviceInstance(SelectMeter);
      }

      if (!meterConnect && !dbcConnect)
      {
        if (!await CheckConnectionsAsync(device, meter))
        {
          return false;
        }
      }
      await device.ConnectableManager.ResetAsync();

      await SettingsMeter(meter);

      var selfTestChecker = device.SelfTestManager;

      if (selfTestChecker == null)
      {
        LogError("Ошибка: Устройство не поддерживает самоконтроль.");
        return false;
      }

      LogInformation($"Начинаем самоконтроль: {testType}");

      var contacts = selfTestChecker.GetValidBusContacts(testType);
      if (contacts == null || contacts.Count == 0)
      {
        LogError($"Ошибка: Не удалось получить список контактов для {testType}.");
        return false;
      }

      bool allTestsPassed = true;

      foreach (int busContact in contacts)
      {

        Console.WriteLine();

        string circuitName = selfTestChecker.GetCircuitName(testType, busContact);
        LogInformation($"Проверка: {circuitName}");

        if (!await PerformCircuitTestAsync(selfTestChecker, meter, testType, circuitName, busContact))
        {
          LogError($"Проверка {circuitName} завершилась с ошибкой!");
          allTestsPassed = false;
          continue;
        }
      }

      if (allTestsPassed)
      {
        LogDebug($"Самоконтроль {testType} завершен успешно.");
        return true;
      }
      else
      {
        LogError($"Самоконтроль {testType} завершен с ошибками.");
        return false;
      }
    }

    /// <summary>
    /// Выполняет проверку указанной цепи: замыкает, проверяет целостность цепи и размыкает.
    /// </summary>
    /// <param name="selfTestChecker">Объект для тестирования.</param>
    /// <param name="meter">Измерительный прибор.</param>
    /// <param name="testType">Тип цепи (BlockingRelay, ADC, Multimeter и т. д.).</param>
    /// <param name="circuitName">Название цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <returns>True, если тест пройден успешно, иначе false.</returns>
    private static async Task<bool> PerformCircuitTestAsync(ISelfTestCheckerDeviceBusCommutation selfTestChecker, IFastMeter meter, SwitchingDeviceTypeConnector testType, string circuitName, int busContact)
    {
      LogInformation($"Запуск теста: {circuitName}");

      if (!await selfTestChecker.ExecuteSelfTestAsync(new CancellationToken(), testType, busContact, 1))
      {
        LogError($"Ошибка при замыкании: {circuitName}.");
        return false;
      }

      LogInformation($"Проверка целостности цепи {circuitName}...");

      // Выполняем проверку цепи (если прибор поддерживает тест целостности)
      await Task.Delay(25);
      bool continuityResult = false;
      if (meter.ContinuityManager != null)
      {
        continuityResult = await meter.ContinuityManager.CheckContinuityAsync(true);
        if (continuityResult)
        {
          LogDebug($"Цепь {circuitName} прошла проверку!");

          if (!await PerformRelayCheck(selfTestChecker, testType, circuitName, busContact, meter))
          {
            LogError($"Ошибка при проверке реле для {circuitName}!");
          }
        }
        else
        {
          LogError($"Цепь {circuitName} не прошла проверку!");
        }
      }
      else
      {
        LogWarning($"Прибор не поддерживает проверку целостности для {circuitName}. Пропуск теста.");
      }

      // Размыкаем цепь
      if (!await selfTestChecker.ExecuteSelfTestAsync(new CancellationToken(), testType, busContact, 2))
      {
        LogError($"Ошибка при размыкании: {circuitName}.");
        return false;
      }

      LogDebug($"Цепь {circuitName} успешно разомкнута.");
      return continuityResult;
    }


    /// <summary>
    /// Проверяет главные реле в цепи самоконтроля для указанного типа проверки.
    /// </summary>
    /// <param name="selfTestChecker">Объект для тестирования.</param>
    /// <param name="testType">Тип тестируемой цепи (например, BlockingRelay, ADC, Multimeter).</param>
    /// <param name="circuitName">Название цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <returns>True, если все реле прошли проверку, иначе false.</returns>
    private static async Task<bool> PerformRelayCheck(ISelfTestCheckerDeviceBusCommutation selfTestChecker, SwitchingDeviceTypeConnector testType, string circuitName, int busContact, IFastMeter meter)
    {
      int relayCount = await selfTestChecker.GetRelayCountAsync(testType, busContact);
      if (relayCount < 0)
      {
        LogError($"Ошибка: Невозможно получить количество реле для {circuitName}.");
        return false;
      }

      LogInformation($"Обнаружено {relayCount} реле в цепи {circuitName}.");
      for (int relay = 1; relay <= relayCount; relay++)
      {
        LogInformation($"Проверка реле {relay} в цепи {circuitName}...");

        await Task.Delay(1);
        if (!await selfTestChecker.ControlRelayAsync(new CancellationToken(), testType, relay, busContact, 2))
        {
          LogError($"Ошибка при включении реле {relay} в цепи {circuitName}.");
          return false;
        }

        LogInformation($"Реле {relay} выключено, проверяем целостность цепи...");

        await Task.Delay(25);
        var result = await meter.ContinuityManager.CheckContinuityAsync(false);
        if (result)
        {
          LogDebug($"Реле {relay} успешно протестировано.");
        }
        else
        {
          LogError($"Ошибка реле {relay}.");
        }

        await Task.Delay(1);
        if (!await selfTestChecker.ControlRelayAsync(new CancellationToken(), testType, relay, busContact, 1))
        {
          LogError($"Ошибка при выключении реле {relay} в цепи {circuitName}.");
          return false;
        }

      }

      return true;
    }


    private static async Task SettingsMeter(IFastMeter meter)
    {
      await meter.ContinuityManager.SetContinuityModeAsync();
    }
    private static async Task<bool> CheckConnectionsAsync(ISwitchingDevice device, IFastMeter meter)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("Проверка подключения устройств");
      var result1 = await device.ConnectableManager.InitializeAsync();
      var result2 = await meter.ConnectableManager.InitializeAsync();
      Console.ForegroundColor = ConsoleColor.White;

      if (result1.Connect && result2.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Оба устройства подключены");
        meterConnect = true;
        dbcConnect = true;
        return true;
      }
      else if (!result1.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("УКШ не подключено");
        Console.ForegroundColor = ConsoleColor.White;
      }
      else if (!result2.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Мультиметр не подключен");
        Console.ForegroundColor = ConsoleColor.White;
      }
      Console.ForegroundColor = ConsoleColor.White;
      return false;
    }

    private static ISwitchingDevice SelectDeviceBusCommutation()
    {
      var dbc = GetSwitchingDevicesAsync().GetAwaiter().GetResult();

      if (dbc == null || !dbc.Any())
      {
        Console.WriteLine("Нет доступных устройств.");
        return null;
      }

      Console.WriteLine("Выберите устройство для самоконтроля блокировочных реле:");

      int index = 1;
      foreach (var device in dbc)
      {
        Console.WriteLine($"{index}. {device.Name} (Номер шасси: {device.NumberChassis}, Номер устройства: {device.Number})");
        index++;
      }

      Console.Write("Введите номер устройства: ");
      if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= dbc.Count)
      {
        var selectedDevice = dbc.ElementAt(choice - 1);
        Console.WriteLine($"Выбрано устройство: {selectedDevice.Name} (ID: {selectedDevice.Id})");
        return selectedDevice;
      }
      else
      {
        Console.WriteLine("Некорректный выбор.");
      }

      return null;
    }

    private static async Task<List<ISwitchingDevice>> GetSwitchingDevicesAsync()
    {
      return _ukshDatabaseSource switch
      {
        UkshDatabaseSource.OldDatabase => await SwitchingDevices.GetAllAsync(),
        UkshDatabaseSource.NewDatabase => GetSwitchingDevicesFromNewDatabase(),
        _ => new List<ISwitchingDevice>(),
      };
    }

    private static List<ISwitchingDevice> GetSwitchingDevicesFromNewDatabase()
    {
      DatabaseEngineInitializer.InitializeAsync().GetAwaiter().GetResult();
      return EngineSwitchingDevices.GetAllAsync().GetAwaiter().GetResult();
    }

    private static UkshDatabaseSource SelectUkshDatabaseSource()
    {
      Console.WriteLine("Выберите источник УКШ:");
      Console.WriteLine("1. Старая БД");
      Console.WriteLine("2. Новая БД");

      while (true)
      {
        Console.Write("Введите номер источника: ");
        if (int.TryParse(Console.ReadLine(), out var choice))
        {
          if (choice == 1)
          {
            return UkshDatabaseSource.OldDatabase;
          }

          if (choice == 2)
          {
            return UkshDatabaseSource.NewDatabase;
          }
        }

        Console.WriteLine("Неверный выбор. Попробуйте снова.");
      }
    }

    private static string GetUkshDatabaseSourceName(UkshDatabaseSource source)
    {
      return source switch
      {
        UkshDatabaseSource.OldDatabase => "старая БД",
        UkshDatabaseSource.NewDatabase => "новая БД",
        _ => "неизвестно",
      };
    }

    private static IFastMeter SelectMeter()
    {
      var dbc = FastMeters.GetAllAsync().GetAwaiter().GetResult();

      if (dbc == null || !dbc.Any())
      {
        Console.WriteLine("Нет доступных устройств.");
        return null;
      }

      Console.WriteLine("Выберите мультиметр:");

      int index = 1;
      foreach (var device in dbc)
      {
        Console.WriteLine($"{index}. {device.Name} (Номер шасси: {device.NumberChassis}, Номер устройства: {device.Number})");
        index++;
      }

      Console.Write("Введите номер устройства: ");
      if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= dbc.Count)
      {
        var selectedDevice = dbc.ElementAt(choice - 1);
        Console.WriteLine($"Выбрано устройство: {selectedDevice.Name} (ID: {selectedDevice.Id})");
        return selectedDevice;
      }
      else
      {
        Console.WriteLine("Некорректный выбор.");
      }

      return null;
    }

    public static T GetDeviceInstance<T>(Func<T> selectDevice) where T : class, IDevice
    {
      var device = selectDevice();
      if (device == null)
      {
        Console.WriteLine("Ошибка: Устройство не выбрано или отсутствует в БД.");
        return null;
      }

      object instance = CreateDeviceInstance(device.DeviceClass);
      if (instance == null || !(instance is T))
      {
        Console.WriteLine($"Ошибка: Не удалось создать объект {device.DeviceClass}.");
        return null;
      }

      CopyProperties(device, instance);

      return instance as T;
    }

    private static object CreateDeviceInstance(string className)
    {
      Console.WriteLine($"Создание swобъекта класса: {className}");

      Type type = Type.GetType(className);
      if (type == null && className.StartsWith("NewCore.", StringComparison.Ordinal))
      {
        try
        {
          type = Assembly.Load(new AssemblyName("Ask.Device.Runtime")).GetType(className);
        }
        catch
        {
          // Игнорируем и переходим к поиску среди уже загруженных сборок.
        }
      }

      if (type == null)
      {
        type = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Select(a => a.GetType(className))
                        .FirstOrDefault(t => t != null);
      }

      if (type == null)
      {
        Console.WriteLine($"Ошибка: Класс {className} не найден.");
        return null;
      }

      return Activator.CreateInstance(type);
    }

    public static void CopyProperties(object source, object target)
    {
      if (source == null || target == null) return;

      Type sourceType = source.GetType();
      Type targetType = target.GetType();

      foreach (PropertyInfo sourceProp in sourceType.GetProperties())
      {
        PropertyInfo targetProp = targetType.GetProperty(sourceProp.Name);
        if (targetProp != null && targetProp.CanWrite)
        {
          object value = sourceProp.GetValue(source);
          if (value != null)
          {
            targetProp.SetValue(target, value);
          }
        }
      }
    }
  }
}
