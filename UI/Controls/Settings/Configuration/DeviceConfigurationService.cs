using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.DataBase.Engine.Static;
using Ask.DataBase.Engine.Static.Devices;
using Ask.DataBase.Provider.Context;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UI.Controls.Settings.Configuration;

/// <summary>
/// Сервис импорта и экспорта конфигурации устройств.
/// </summary>
public static class DeviceConfigurationService
{
  private static readonly JsonSerializerOptions ExportJsonOptions = CreateJsonOptions(writeIndented: true);
  private static readonly JsonSerializerOptions ImportJsonOptions = CreateJsonOptions(writeIndented: false);

  /// <summary>
  /// Экспортирует текущую конфигурацию устройств в JSON-файл.
  /// </summary>
  public static async Task ExportToFileAsync(string filePath, CancellationToken cancellationToken = default)
  {
    var configuration = await BuildConfigurationFileAsync(cancellationToken);
    string json = JsonSerializer.Serialize(configuration, ExportJsonOptions);
    await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken);
  }

  /// <summary>
  /// Импортирует конфигурацию устройств из JSON-файла.
  /// </summary>
  public static async Task ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
  {
    string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
    var configuration = ParseConfigurationFile(json);
    ValidateImportedConfiguration(configuration);
    await ApplyConfigurationFileAsync(configuration, cancellationToken);
  }

  /// <summary>
  /// Собирает текущую конфигурацию устройств из БД.
  /// </summary>
  public static async Task<DeviceConfigurationFileModel> BuildConfigurationFileAsync(CancellationToken cancellationToken = default)
  {
    var chassisTask = ChassisManagers.GetAllAsync(cancellationToken);
    var racksTask = Racks.GetAllAsync(cancellationToken);
    var relayTask = RelaySwitchModules.GetAllAsync(cancellationToken);
    var switchingTask = SwitchingDevices.GetAllAsync(cancellationToken);
    var powerTask = PowerSourceModules.GetAllAsync(cancellationToken);
    var fastMetersTask = FastMeters.GetAllAsync(cancellationToken);
    var breakdownTask = BreakdownTesters.GetAllAsync(cancellationToken);

    await Task.WhenAll(chassisTask, racksTask, relayTask, switchingTask, powerTask, fastMetersTask, breakdownTask);

    return new DeviceConfigurationFileModel
    {
      Version = 1,
      ExportedAtUtc = DateTime.UtcNow,
      Chassis = chassisTask.Result
        .OrderBy(x => x.Number)
        .Select(ToDto)
        .ToList(),
      Racks = racksTask.Result
        .OrderBy(x => x.NumberChassis)
        .ThenBy(x => x.Number)
        .Select(ToDto)
        .ToList(),
      RelaySwitchModules = relayTask.Result
        .OrderBy(x => x.NumberChassis)
        .ThenBy(x => x.Number)
        .Select(ToDto)
        .ToList(),
      SwitchingDevices = switchingTask.Result
        .OrderBy(x => x.NumberChassis)
        .ThenBy(x => x.Number)
        .Select(ToDto)
        .ToList(),
      PowerSourceModules = powerTask.Result
        .OrderBy(x => x.NumberChassis)
        .ThenBy(x => x.Number)
        .Select(ToDto)
        .ToList(),
      FastMeters = fastMetersTask.Result
        .OrderBy(x => x.NumberChassis)
        .ThenBy(x => x.Number)
        .Select(ToDto)
        .ToList(),
      BreakdownTesters = breakdownTask.Result
        .OrderBy(x => x.NumberChassis)
        .ThenBy(x => x.Number)
        .Select(ToDto)
        .ToList(),
    };
  }

  internal static DeviceConfigurationFileModel ParseConfigurationFile(string json)
  {
    var model = JsonSerializer.Deserialize<DeviceConfigurationFileModel>(json, ImportJsonOptions)
      ?? throw new InvalidDataException("Не удалось прочитать JSON конфигурации.");

    model.Chassis ??= [];
    model.Racks ??= [];
    model.RelaySwitchModules ??= [];
    model.SwitchingDevices ??= [];
    model.PowerSourceModules ??= [];
    model.FastMeters ??= [];
    model.BreakdownTesters ??= [];

    return model;
  }

  internal static void ValidateImportedConfiguration(DeviceConfigurationFileModel model)
  {
    EnsureNoDuplicates(model.Chassis, chassis => chassis.Number.ToString(CultureInfo.InvariantCulture), "шасси");
    EnsureNoDuplicates(model.Racks, rack => $"{rack.NumberChassis}:{rack.Number}", "стойки");
    EnsureNoDuplicates(model.RelaySwitchModules, device => $"{device.NumberChassis}:{device.Number}", "модули коммутации реле");
    EnsureNoDuplicates(model.SwitchingDevices, device => $"{device.NumberChassis}:{device.Number}", "устройства коммутации шин");
    EnsureNoDuplicates(model.PowerSourceModules, device => $"{device.NumberChassis}:{device.Number}", "модули источника питания");
    EnsureNoDuplicates(model.FastMeters, device => $"{device.NumberChassis}:{device.Number}", "быстрые измерители");
    EnsureNoDuplicates(model.BreakdownTesters, device => $"{device.NumberChassis}:{device.Number}", "пробойные установки");

    EnsureDeviceClassSet(model.Chassis, item => item.DeviceClass, "шасси");
    EnsureDeviceClassSet(model.Racks, item => item.DeviceClass, "стойки");
    EnsureDeviceClassSet(model.RelaySwitchModules, item => item.DeviceClass, "модули коммутации реле");
    EnsureDeviceClassSet(model.SwitchingDevices, item => item.DeviceClass, "устройства коммутации шин");
    EnsureDeviceClassSet(model.PowerSourceModules, item => item.DeviceClass, "модули источника питания");
    EnsureDeviceClassSet(model.FastMeters, item => item.DeviceClass, "быстрые измерители");
    EnsureDeviceClassSet(model.BreakdownTesters, item => item.DeviceClass, "пробойные установки");

    var chassisNumbers = model.Chassis
      .Select(chassis => chassis.Number)
      .ToHashSet();

    bool hasLinkedDevices =
      model.Racks.Count > 0 ||
      model.RelaySwitchModules.Count > 0 ||
      model.SwitchingDevices.Count > 0 ||
      model.PowerSourceModules.Count > 0 ||
      model.FastMeters.Count > 0 ||
      model.BreakdownTesters.Count > 0;

    if (chassisNumbers.Count == 0)
    {
      if (hasLinkedDevices)
      {
        throw new InvalidDataException("В JSON есть устройства, но отсутствуют шасси.");
      }

      return;
    }

    ValidateLinkedDevices(model.Racks.Select(rack => rack.NumberChassis), chassisNumbers, "стойки");
    ValidateLinkedDevices(model.RelaySwitchModules.Select(device => device.NumberChassis), chassisNumbers, "модули коммутации реле");
    ValidateLinkedDevices(model.SwitchingDevices.Select(device => device.NumberChassis), chassisNumbers, "устройства коммутации шин");
    ValidateLinkedDevices(model.PowerSourceModules.Select(device => device.NumberChassis), chassisNumbers, "модули источника питания");
    ValidateLinkedDevices(model.FastMeters.Select(device => device.NumberChassis), chassisNumbers, "быстрые измерители");
    ValidateLinkedDevices(model.BreakdownTesters.Select(device => device.NumberChassis), chassisNumbers, "пробойные установки");
  }

  internal static async Task ApplyConfigurationFileAsync(
    DeviceConfigurationFileModel model,
    CancellationToken cancellationToken = default)
  {
    await using var db = new AppDbContext();
    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

    db.FastMeters.RemoveRange(db.FastMeters);
    db.PowerSourceModules.RemoveRange(db.PowerSourceModules);
    db.RelaySwitchModules.RemoveRange(db.RelaySwitchModules);
    db.SwitchingDevices.RemoveRange(db.SwitchingDevices);
    db.BreakdownTesters.RemoveRange(db.BreakdownTesters);
    db.Rack.RemoveRange(db.Rack);
    db.ChassisManagers.RemoveRange(db.ChassisManagers);
    await db.SaveChangesAsync(cancellationToken);

    db.Rack.AddRange(model.Racks);
    db.ChassisManagers.AddRange(model.Chassis);
    db.BreakdownTesters.AddRange(model.BreakdownTesters);
    db.RelaySwitchModules.AddRange(model.RelaySwitchModules);
    db.SwitchingDevices.AddRange(model.SwitchingDevices);
    db.PowerSourceModules.AddRange(model.PowerSourceModules);
    db.FastMeters.AddRange(model.FastMeters);
    await db.SaveChangesAsync(cancellationToken);

    await transaction.CommitAsync(cancellationToken);
    DeviceRuntime.ClearCache();
  }

  private static JsonSerializerOptions CreateJsonOptions(bool writeIndented)
  {
    var options = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true,
      WriteIndented = writeIndented
    };

    options.Converters.Add(new JsonStringEnumConverter());
    return options;
  }

  private static void EnsureNoDuplicates<T>(IEnumerable<T> items, Func<T, string> keySelector, string sectionName)
  {
    var duplicate = items
      .GroupBy(keySelector)
      .FirstOrDefault(group => group.Count() > 1);

    if (duplicate != null)
    {
      throw new InvalidDataException($"В разделе \"{sectionName}\" найдены дубликаты: {duplicate.Key}.");
    }
  }

  private static void EnsureDeviceClassSet<T>(IEnumerable<T> items, Func<T, string?> deviceClassSelector, string sectionName)
  {
    if (items.Any(item => string.IsNullOrWhiteSpace(deviceClassSelector(item))))
    {
      throw new InvalidDataException($"В разделе \"{sectionName}\" поле DeviceClass обязательно для всех записей.");
    }
  }

  private static void ValidateLinkedDevices(IEnumerable<int> linkedChassisNumbers, ISet<int> availableChassis, string sectionName)
  {
    var unknownChassis = linkedChassisNumbers
      .Where(number => !availableChassis.Contains(number))
      .Distinct()
      .OrderBy(number => number)
      .ToList();

    if (unknownChassis.Count == 0)
    {
      return;
    }

    throw new InvalidDataException(
      $"В разделе \"{sectionName}\" есть привязка к несуществующим шасси: {string.Join(", ", unknownChassis)}.");
  }

  private static ChassisManagerDto ToDto(IChassisManager item) => item.Convert();

  private static RackDto ToDto(IRack item) => item.Convert();

  private static RelaySwitchModuleDto ToDto(IRelaySwitchModule item) => item.Convert();

  private static SwitchingDeviceDto ToDto(ISwitchingDevice item) => item.Convert();

  private static PowerSourceModuleDto ToDto(IPowerSourceModule item) => item.Convert();

  private static FastMeterDto ToDto(IFastMeter item) => item.Convert();

  private static BreakdownTesterDto ToDto(IBreakdownTester item) => item.Convert();
}
