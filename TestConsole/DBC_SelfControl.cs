using System.Reflection;
using AppManager.DataBase.Services;
using NewCore.Base.Device;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;
using static Utilities.LoggerUtility;

namespace TestConsole
{
  internal class DBC_SelfControl
  {

    private static bool meterConnect = false;
    private static bool dbcConnect = false;
    internal static async Task RunAsync()
    {
      Console.WriteLine("=== Самоконтроль УКШ ===");
      while (true)
      {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\nВыберите действие:");
        Console.WriteLine("1. Тест блокировочных реле");
        Console.WriteLine("2. Тест мультиметра");
        Console.WriteLine("3. Тест АЦП");
        Console.WriteLine("4. Тест АЦП с переполюсовкой");
        Console.WriteLine("5. Тест ПИНТ");
        Console.WriteLine("6. Тест Шунт");
        Console.WriteLine("7. Тест ППУ");
        Console.WriteLine("8. Весь самоконтроль");
        Console.WriteLine("0. Выход");

        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 8)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        meterConnect = false;
        dbcConnect = false;

        switch (choice)
        {
          case 1:
            await SelfCheckCircuitAsync(TypeConnector.BlockingRelay);
            break;

          case 2:
            await SelfCheckCircuitAsync(TypeConnector.Multimeter);
            break;

          case 3:
            await SelfCheckCircuitAsync(TypeConnector.ADC);
            break;

          case 4:
            await SelfCheckCircuitAsync(TypeConnector.ADCReversed);
            break;

          case 5:
            await SelfCheckCircuitAsync(TypeConnector.PINT);
            break;

          case 6:
            await SelfCheckCircuitAsync(TypeConnector.Shunt);
            break;

          case 7:
            await SelfCheckCircuitAsync(TypeConnector.BreakdownTester);
            break;

          case 8:

            var device = GetDeviceInstance(SelectDeviceBusCommutation);
            var meter = GetDeviceInstance(SelectMeter);

            LogInformation("Начинаем полный самоконтроль всех цепей...");

            foreach (TypeConnector testType in Enum.GetValues(typeof(TypeConnector)))
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
    private static async Task<bool> SelfCheckCircuitAsync(TypeConnector testType, ISwitchingDevice device = null, IFastMeter meter = null)
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
    private static async Task<bool> PerformCircuitTestAsync(ISelfTestChecker selfTestChecker, IFastMeter meter, TypeConnector testType, string circuitName, int busContact)
    {
      LogInformation($"Запуск теста: {circuitName}");

      if (!await selfTestChecker.ExecuteSelfTestAsync(testType, busContact, 1))
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
        continuityResult = await meter.ContinuityManager.CheckContinuityAsync();
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
      if (!await selfTestChecker.ExecuteSelfTestAsync(testType, busContact, 2))
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
    private static async Task<bool> PerformRelayCheck(ISelfTestChecker selfTestChecker, TypeConnector testType, string circuitName, int busContact, IFastMeter meter)
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
        if (!await selfTestChecker.ControlRelayAsync(testType, relay, busContact, 2))
        {
          LogError($"Ошибка при включении реле {relay} в цепи {circuitName}.");
          return false;
        }

        LogInformation($"Реле {relay} выключено, проверяем целостность цепи...");

        await Task.Delay(25);
        var result = await meter.ContinuityManager.CheckContinuityAsync();
        if (!result)
        {
          LogDebug($"Реле {relay} успешно протестировано.");
        }
        else
        {
          LogError($"Ошибка реле {relay}.");
        }

        await Task.Delay(1);
        if (!await selfTestChecker.ControlRelayAsync(testType, relay, busContact, 1))
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
      var dbc = new SwitchingDeviceServices().GetAll();

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

    private static IFastMeter SelectMeter()
    {
      var dbc = new FastMeterServices().GetAll();

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
