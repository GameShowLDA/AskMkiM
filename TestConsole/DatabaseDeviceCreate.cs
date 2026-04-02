using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.DataBase.Engine.Factory;
using Ask.DataBase.Engine.Initialization;
using Ask.DataBase.Engine.Static;
using Ask.Device.Runtime.Device;
using System.Reflection;

namespace TestConsole;

internal static class DatabaseDeviceCreate
{
  private static readonly MethodInfo CreateMethod = GetGenericMethod(nameof(DeviceRuntime.CreateAsync), parameterCount: 2);
  private static readonly MethodInfo GetByNumberMethod = GetGenericMethod(nameof(DeviceRuntime.GetByNumberAsync), parameterCount: 2);
  private static readonly MethodInfo GetByChassisAndNumberMethod = GetGenericMethod(nameof(DeviceRuntime.GetDeviceByNumberChassisAsync), parameterCount: 3);
  private static readonly MethodInfo ValidateDeviceClassMethod = typeof(DeviceFactory)
    .GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(x => x.Name == nameof(DeviceFactory.Create) && x.IsGenericMethodDefinition && x.GetParameters().Length == 1);

  private static readonly IReadOnlyList<DeviceOption> DeviceOptions = LoadDeviceOptions();

  public static async Task RunAsync()
  {
    Console.WriteLine();
    Console.WriteLine("=== Добавление устройства в БД ===");

    await DatabaseEngineInitializer.InitializeAsync(WriteLog);

    if (DeviceOptions.Count == 0)
    {
      WriteError("Доступные runtime-устройства не найдены.");
      return;
    }

    Console.WriteLine("Доступные интерфейсы и модели:");
    for (var i = 0; i < DeviceOptions.Count; i++)
    {
      var option = DeviceOptions[i];
      Console.WriteLine($"{i + 1}. {option.DisplayName}");
    }

    Console.WriteLine("0. Назад");

    var choice = ReadMenuChoice(0, DeviceOptions.Count);
    if (choice == 0)
    {
      return;
    }

    var selected = DeviceOptions[choice - 1];
    var device = CreateRuntimeDevice(selected.RuntimeType);

    Console.WriteLine();
    Console.WriteLine($"Выбран интерфейс: {selected.InterfaceType.Name}");
    Console.WriteLine($"Выбран runtime-класс: {selected.RuntimeType.FullName}");

    FillCommonFields(device);
    ApplySpecificSettings(selected.InterfaceType, device);

    if (!ValidateDeviceClass(selected.InterfaceType, device.DeviceClass))
    {
      return;
    }

    var existing = await FindExistingDeviceAsync(selected.InterfaceType, device);
    if (existing != null)
    {
      PrintDuplicateMessage(existing);
      return;
    }

    try
    {
      var created = await CreateDeviceAsync(selected.InterfaceType, device);
      PrintSummary(selected, created);
    }
    catch (Exception ex)
    {
      WriteError($"Ошибка добавления устройства: {ex.Message}");
    }
  }

  private static IReadOnlyList<DeviceOption> LoadDeviceOptions()
  {
    var assembly = typeof(ManagerChassis).Assembly;

    return assembly
      .GetTypes()
      .Where(x =>
        x.IsClass &&
        !x.IsAbstract &&
        x.IsPublic &&
        x.Namespace == typeof(ManagerChassis).Namespace &&
        typeof(IDevice).IsAssignableFrom(x) &&
        x.GetConstructor(Type.EmptyTypes) != null)
      .Select(CreateDeviceOption)
      .Where(x => x != null)
      .Cast<DeviceOption>()
      .OrderBy(x => x.SortOrder)
      .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static DeviceOption? CreateDeviceOption(Type runtimeType)
  {
    var interfaceType = ResolveMainDeviceInterface(runtimeType);
    if (interfaceType == null)
    {
      return null;
    }

    var device = CreateRuntimeDevice(runtimeType);
    return new DeviceOption(
      interfaceType,
      runtimeType,
      $"{GetDisplayName(device.DeviceType)} [{interfaceType.Name}] -> {runtimeType.Name}",
      GetSortOrder(device.DeviceType));
  }

  private static Type? ResolveMainDeviceInterface(Type runtimeType)
  {
    return runtimeType
      .GetInterfaces()
      .Where(IsRootDeviceInterface)
      .OrderByDescending(GetInterfaceDepth)
      .FirstOrDefault();
  }

  private static bool IsRootDeviceInterface(Type interfaceType)
  {
    if (!interfaceType.IsInterface || !typeof(IDevice).IsAssignableFrom(interfaceType))
    {
      return false;
    }

    return interfaceType != typeof(IDevice)
      && interfaceType != typeof(IHeadUnit)
      && interfaceType != typeof(IAttachableDevice);
  }

  private static int GetInterfaceDepth(Type interfaceType)
  {
    return interfaceType
      .GetInterfaces()
      .Count(x => typeof(IDevice).IsAssignableFrom(x));
  }

  private static IDevice CreateRuntimeDevice(Type runtimeType)
  {
    return (IDevice)(Activator.CreateInstance(runtimeType)
      ?? throw new InvalidOperationException($"Не удалось создать runtime-класс '{runtimeType.FullName}'."));
  }

  private static void FillCommonFields(IDevice device)
  {
    device.Name = ReadString("Имя", device.Name);
    device.Description = ReadString("Описание", device.Description);
    device.Number = ReadInt("Номер устройства", device.Number > 0 ? device.Number : 1);
    device.ConnectionDetails = ReadString("Подключение", device.ConnectionDetails);
    device.DeviceClass = ReadString("Класс устройства", device.DeviceClass);

    if (device is IAttachableDevice attachableDevice)
    {
      attachableDevice.NumberChassis = ReadInt(
        "Номер шасси",
        attachableDevice.NumberChassis > 0 ? attachableDevice.NumberChassis : 1);
    }
  }

  private static void ApplySpecificSettings(Type interfaceType, IDevice device)
  {
    if (interfaceType == typeof(IChassisManager))
    {
      var chassis = (IChassisManager)device;
      chassis.BusType = ReadEnum("Тип структурной шины", chassis.BusType);
      return;
    }

    if (interfaceType == typeof(IRelaySwitchModule))
    {
      var relay = (IRelaySwitchModule)device;
      SetIntProperty(device, "NumberRack", ReadInt("Номер стойки", GetIntProperty(device, "NumberRack", 1)));
      relay.PointCount = ReadInt("Количество точек", relay.PointCount > 0 ? relay.PointCount : 350);
      relay.BusType = ReadEnum("Тип шины", relay.BusType);
      relay.SwitchResistance = ReadDouble("Сопротивление коммутатора", relay.SwitchResistance);
      relay.SwitchCapacitance = ReadDouble("Собственная емкость коммутатора", relay.SwitchCapacitance);
      return;
    }

    if (interfaceType == typeof(IPowerSourceModule))
    {
      var powerSource = (IPowerSourceModule)device;
      powerSource.ResistanceCalibrationJson = ReadString(
        "JSON калибровки сопротивления",
        powerSource.ResistanceCalibrationJson ?? string.Empty);
      return;
    }

    if (interfaceType == typeof(IFastMeter))
    {
      var fastMeter = (IFastMeter)device;
      fastMeter.TypeMode = ReadEnum("Режим мультиметра", fastMeter.TypeMode);
      fastMeter.MaxContinuityResistance = ReadInt(
        "Максимальное сопротивление прозвонки",
        fastMeter.MaxContinuityResistance > 0 ? fastMeter.MaxContinuityResistance : 100000);
      return;
    }

    if (interfaceType == typeof(IBreakdownTester))
    {
      var breakdownTester = (IBreakdownTester)device;
      breakdownTester.Mode = ReadEnum("Режим ППУ", breakdownTester.Mode);
      breakdownTester.PiMaxVoltage = ReadInt(
        "Максимальное напряжение ПИ",
        breakdownTester.PiMaxVoltage > 0 ? breakdownTester.PiMaxVoltage : 700);
      breakdownTester.SiMaxVoltage = ReadInt(
        "Максимальное напряжение СИ",
        breakdownTester.SiMaxVoltage > 0 ? breakdownTester.SiMaxVoltage : 1000);
      breakdownTester.IRMinVoltage = ReadInt(
        "Минимальное напряжение ИС",
        breakdownTester.IRMinVoltage > 0 ? breakdownTester.IRMinVoltage : 50);
      return;
    }

    if (interfaceType == typeof(IUninterruptiblePowerSupply))
    {
      var ups = (IUninterruptiblePowerSupply)device;
      ups.LastResolvedDevicePath = ReadString("Последний путь устройства", ups.LastResolvedDevicePath);
    }
  }

  private static bool ValidateDeviceClass(Type interfaceType, string deviceClass)
  {
    try
    {
      ValidateDeviceClassMethod.MakeGenericMethod(interfaceType).Invoke(null, [deviceClass]);
      return true;
    }
    catch (TargetInvocationException ex)
    {
      WriteError($"Класс устройства недоступен: {ex.InnerException?.Message ?? ex.Message}");
      return false;
    }
    catch (Exception ex)
    {
      WriteError($"Класс устройства недоступен: {ex.Message}");
      return false;
    }
  }

  private static async Task<IDevice?> FindExistingDeviceAsync(Type interfaceType, IDevice device)
  {
    if (typeof(IAttachableDevice).IsAssignableFrom(interfaceType) && device is IAttachableDevice attachableDevice)
    {
      var task = (Task)GetByChassisAndNumberMethod
        .MakeGenericMethod(interfaceType)
        .Invoke(null, [attachableDevice.NumberChassis, device.Number, CancellationToken.None])!;

      return (IDevice?)await AwaitTaskResultAsync(task);
    }

    var getByNumberTask = (Task)GetByNumberMethod
      .MakeGenericMethod(interfaceType)
      .Invoke(null, [device.Number, CancellationToken.None])!;

    return (IDevice?)await AwaitTaskResultAsync(getByNumberTask);
  }

  private static async Task<IDevice> CreateDeviceAsync(Type interfaceType, IDevice device)
  {
    var task = (Task)CreateMethod
      .MakeGenericMethod(interfaceType)
      .Invoke(null, [device, CancellationToken.None])!;

    return (IDevice)(await AwaitTaskResultAsync(task)
      ?? throw new InvalidOperationException("Движок не вернул созданное устройство."));
  }

  private static async Task<object?> AwaitTaskResultAsync(Task task)
  {
    await task;
    return task.GetType().GetProperty("Result")?.GetValue(task);
  }

  private static void PrintDuplicateMessage(IDevice existingDevice)
  {
    if (existingDevice is IAttachableDevice attachableDevice)
    {
      WriteError(
        $"Устройство с шасси {attachableDevice.NumberChassis} и адресом {existingDevice.Number} уже существует.");
      return;
    }

    WriteError($"Устройство с номером {existingDevice.Number} уже существует.");
  }

  private static void PrintSummary(DeviceOption option, IDevice device)
  {
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Устройство успешно добавлено.");
    Console.ResetColor();

    Console.WriteLine("Итог:");
    Console.WriteLine($"  Интерфейс: {option.InterfaceType.Name}");
    Console.WriteLine($"  Runtime-класс: {option.RuntimeType.FullName}");
    Console.WriteLine($"  Id: {device.Id}");
    Console.WriteLine($"  Тип: {device.DeviceType}");
    Console.WriteLine($"  Имя: {device.Name}");
    Console.WriteLine($"  Номер: {device.Number}");

    if (device is IAttachableDevice attachableDevice)
    {
      Console.WriteLine($"  Номер шасси: {attachableDevice.NumberChassis}");
    }

    var numberRack = GetIntProperty(device, "NumberRack", 0);
    if (numberRack > 0)
    {
      Console.WriteLine($"  Номер стойки: {numberRack}");
    }

    Console.WriteLine($"  Подключение: {device.ConnectionDetails}");
    Console.WriteLine($"  DeviceClass: {device.DeviceClass}");
  }

  private static int GetIntProperty(object instance, string propertyName, int defaultValue)
  {
    var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
    if (property?.PropertyType == typeof(int) && property.CanRead)
    {
      return (int)(property.GetValue(instance) ?? defaultValue);
    }

    return defaultValue;
  }

  private static void SetIntProperty(object instance, string propertyName, int value)
  {
    var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
    if (property?.PropertyType == typeof(int) && property.CanWrite)
    {
      property.SetValue(instance, value);
    }
  }

  private static string ReadString(string label, string defaultValue = "")
  {
    Console.Write($"{label}{FormatDefault(defaultValue)}: ");
    var input = Console.ReadLine()?.Trim();
    return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
  }

  private static int ReadInt(string label, int defaultValue = 0)
  {
    while (true)
    {
      Console.Write($"{label}{FormatDefault(defaultValue == 0 ? string.Empty : defaultValue.ToString())}: ");
      var input = Console.ReadLine()?.Trim();

      if (string.IsNullOrWhiteSpace(input))
      {
        return defaultValue;
      }

      if (int.TryParse(input, out var value))
      {
        return value;
      }

      WriteError("Введите целое число.");
    }
  }

  private static double ReadDouble(string label, double defaultValue = 0)
  {
    while (true)
    {
      Console.Write($"{label}{FormatDefault(defaultValue == 0 ? string.Empty : defaultValue.ToString("G"))}: ");
      var input = Console.ReadLine()?.Trim();

      if (string.IsNullOrWhiteSpace(input))
      {
        return defaultValue;
      }

      if (double.TryParse(input, out var value))
      {
        return value;
      }

      WriteError("Введите число.");
    }
  }

  private static TEnum ReadEnum<TEnum>(string label, TEnum defaultValue)
    where TEnum : struct, Enum
  {
    var values = Enum.GetValues<TEnum>();

    while (true)
    {
      Console.WriteLine($"{label}:");
      for (var i = 0; i < values.Length; i++)
      {
        Console.WriteLine($"  {i + 1}. {values[i]}");
      }

      var defaultIndex = Array.IndexOf(values, defaultValue) + 1;
      Console.Write($"Выберите значение{FormatDefault(defaultIndex > 0 ? defaultIndex.ToString() : string.Empty)}: ");
      var input = Console.ReadLine()?.Trim();

      if (string.IsNullOrWhiteSpace(input) && defaultIndex > 0)
      {
        return defaultValue;
      }

      if (int.TryParse(input, out var index) && index >= 1 && index <= values.Length)
      {
        return values[index - 1];
      }

      WriteError("Неверный выбор.");
    }
  }

  private static int ReadMenuChoice(int minValue, int maxValue)
  {
    while (true)
    {
      Console.Write("Введите номер действия: ");
      if (int.TryParse(Console.ReadLine(), out var choice) && choice >= minValue && choice <= maxValue)
      {
        return choice;
      }

      WriteError("Неверный выбор.");
    }
  }

  private static string FormatDefault(string value)
  {
    return string.IsNullOrWhiteSpace(value) ? string.Empty : $" [{value}]";
  }

  private static string GetDisplayName(object deviceType)
  {
    return deviceType.ToString() switch
    {
      "ChassisManager" => "Шасси",
      "RelaySwitchModule" => "Модуль коммутации реле",
      "PowerSourceModule" => "Модуль источника питания",
      "SwitchingDevice" => "Устройство коммутации",
      "FastMeter" => "Быстрый измеритель",
      "BreakdownTester" => "Пробойная установка",
      "UninterruptiblePowerSupply" => "Бесперебойник",
      "Rack" => "Стойка",
      _ => deviceType.ToString() ?? "Неизвестное устройство",
    };
  }

  private static int GetSortOrder(object deviceType)
  {
    return deviceType.ToString() switch
    {
      "ChassisManager" => 10,
      "Rack" => 20,
      "RelaySwitchModule" => 30,
      "PowerSourceModule" => 40,
      "SwitchingDevice" => 50,
      "FastMeter" => 60,
      "BreakdownTester" => 70,
      "UninterruptiblePowerSupply" => 80,
      _ => 999,
    };
  }

  private static MethodInfo GetGenericMethod(string methodName, int parameterCount)
  {
    return typeof(DeviceRuntime)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Single(x => x.Name == methodName && x.IsGenericMethodDefinition && x.GetParameters().Length == parameterCount);
  }

  private static void WriteError(string message)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
  }

  private static void WriteLog(string message)
  {
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine(message);
    Console.ResetColor();
  }

  private sealed record DeviceOption(Type InterfaceType, Type RuntimeType, string DisplayName, int SortOrder);
}
