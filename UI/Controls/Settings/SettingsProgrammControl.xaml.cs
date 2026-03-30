using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.DataBase.Engine.Builder;
using Ask.DataBase.Engine.Static;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Device.Communication.Com.Configuration;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.Localization;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using DataBaseConfiguration;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace UI.Controls.Settings
{
  /// <summary>
  /// Логика взаимодействия для SettingsProgrammControl.xaml
  /// </summary>
  public partial class SettingsProgrammControl : UserControl
  {
    private static readonly JsonSerializerOptions ExportJsonOptions = CreateJsonOptions(writeIndented: true);
    private static readonly JsonSerializerOptions ImportJsonOptions = CreateJsonOptions(writeIndented: false);
    private readonly Action<SystemStateEvents.AdminRightsChanged> _adminRightsChangedHandler;
    private bool _isAdminRightsSubscribed;

    public SettingsProgrammControl()
    {
      InitializeComponent();
      _adminRightsChangedHandler = OnAdminRightsChanged;
      Loaded += SettingsProgrammControl_Loaded;
      Unloaded += SettingsProgrammControl_Unloaded;
    }

    private void SettingsProgrammControl_Loaded(object sender, RoutedEventArgs e)
    {
      LocalizationService.RefreshCurrentLanguage();
      UpdateImportExportVisibility(AdminConfig.GetAdminRights());

      if (_isAdminRightsSubscribed)
      {
        return;
      }

      EventAggregator.Subscribe(_adminRightsChangedHandler);
      _isAdminRightsSubscribed = true;
    }

    private void SettingsProgrammControl_Unloaded(object sender, RoutedEventArgs e)
    {
      if (!_isAdminRightsSubscribed)
      {
        return;
      }

      EventAggregator.Unsubscribe(_adminRightsChangedHandler);
      _isAdminRightsSubscribed = false;
    }

    private void OnAdminRightsChanged(SystemStateEvents.AdminRightsChanged eventData)
    {
      if (Dispatcher.CheckAccess())
      {
        UpdateImportExportVisibility(eventData.IsAdmin);
      }
      else
      {
        Dispatcher.Invoke(() => UpdateImportExportVisibility(eventData.IsAdmin));
      }
    }

    private void UpdateImportExportVisibility(bool isAdmin)
    {
      var visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
      ExportConfigButton.Visibility = visibility;
      ImportConfigButton.Visibility = visibility;
    }

    private void PrintConfig(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var chassisList = ChassisManagers.GetAllAsync().GetAwaiter().GetResult().OrderBy(chassis => chassis.Number)
          .ToList();

        string printableText = BuildPrintableConfiguration(
          chassisList,
          numberChassis => FastMeters.GetDevicesByNumberChassisAsync(numberChassis).GetAwaiter().GetResult(),
          numberChassis => BreakdownTesters.GetDevicesByNumberChassisAsync(numberChassis).GetAwaiter().GetResult(),
          numberChassis => PowerSourceModules.GetDevicesByNumberChassisAsync(numberChassis).GetAwaiter().GetResult(),
          numberChassis => RelaySwitchModules.GetDevicesByNumberChassisAsync(numberChassis).GetAwaiter().GetResult(),
          numberChassis => SwitchingDevices.GetDevicesByNumberChassisAsync(numberChassis).GetAwaiter().GetResult());

        PrintText(printableText);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при формировании конфигурации: {ex.Message}", "Ошибка",
          MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExportConfig(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var saveDialog = new SaveFileDialog
        {
          Title = "Экспорт конфигурации",
          Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*",
          DefaultExt = ".json",
          AddExtension = true,
          OverwritePrompt = true,
          FileName = $"askmkim-config-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };

        if (saveDialog.ShowDialog() != true)
        {
          return;
        }

        var configuration = BuildConfigurationFile();
        string json = JsonSerializer.Serialize(configuration, ExportJsonOptions);
        File.WriteAllText(saveDialog.FileName, json, Encoding.UTF8);

        NotificationHostService.Instance.Show(
          "Экспорт конфигурации",
          $"Конфигурация сохранена в файл:\n{saveDialog.FileName}",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        NotificationHostService.Instance.Show(
          "Ошибка экспорта конфигурации",
          ex.Message,
          NotificationType.Error);
      }
    }

    private void ImportConfig(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var openDialog = new OpenFileDialog
        {
          Title = "Импорт конфигурации",
          Filter = "JSON (*.json)|*.json|Все файлы (*.*)|*.*",
          DefaultExt = ".json",
          CheckFileExists = true,
          Multiselect = false
        };

        if (openDialog.ShowDialog() != true)
        {
          return;
        }

        var confirmation = Message.MessageBoxCustom.Show(
          "При импорте текущая конфигурация устройств будет полностью удалена и заменена содержимым JSON-файла. Продолжить?",
          "Импорт конфигурации",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
          return;
        }

        string json = File.ReadAllText(openDialog.FileName, Encoding.UTF8);
        var configuration = ParseConfigurationFile(json);
        ValidateImportedConfiguration(configuration);
        ApplyConfigurationFile(configuration);

        DeviceConfigManager?.ReloadConfiguration();

        NotificationHostService.Instance.Show(
          "Импорт конфигурации",
          "Конфигурация успешно импортирована.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        NotificationHostService.Instance.Show(
          "Ошибка импорта конфигурации",
          ex.Message,
          NotificationType.Error);
      }
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

    private static DeviceConfigurationFileModel BuildConfigurationFile()
    {
      using var db = DataBaseConfig.Context;

      return new DeviceConfigurationFileModel
      {
        Version = 1,
        ExportedAtUtc = DateTime.UtcNow,
        Chassis = db.ChassisManagers
          .OrderBy(chassis => chassis.Number)
          .Select(chassis => new ChassisConfigurationItem
          {
            Name = chassis.Name,
            Description = chassis.Description,
            Number = chassis.Number,
            ConnectionDetails = chassis.ConnectionDetails,
            DeviceClass = chassis.DeviceClass,
            BusType = chassis.BusType,
          })
          .ToList(),

        Racks = Racks.GetAllAsync().GetAwaiter().GetResult()
          .OrderBy(rack => rack.NumberChassis)
          .ThenBy(rack => rack.Number)
          .Select(rack => new RackDto
          {
            Name = rack.Name,
            Description = rack.Description,
            Number = rack.Number,
            NumberChassis = rack.NumberChassis,
            ConnectionDetails = rack.ConnectionDetails,
            DeviceClass = rack.DeviceClass,
          })
          .ToList(),

        RelaySwitchModules = db.RelaySwitchModules
          .OrderBy(device => device.NumberChassis)
          .ThenBy(device => device.Number)
          .Select(device => new RelaySwitchModuleConfigurationItem
          {
            Name = device.Name,
            Description = device.Description,
            Number = device.Number,
            NumberChassis = device.NumberChassis,
            NumberRack = device.NumberRack,
            PointCount = device.PointCount,
            ConnectionDetails = device.ConnectionDetails,
            DeviceClass = device.DeviceClass,
            SwitchResistance = device.SwitchResistance,
            SwitchCapacitance = device.SwitchCapacitance,
            BusType = device.BusType,
          })
          .ToList(),

        SwitchingDevices = db.SwitchingDevices
          .OrderBy(device => device.NumberChassis)
          .ThenBy(device => device.Number)
          .Select(device => new SwitchingDeviceConfigurationItem
          {
            Name = device.Name,
            Description = device.Description,
            Number = device.Number,
            NumberChassis = device.NumberChassis,
            ConnectionDetails = device.ConnectionDetails,
            DeviceClass = device.DeviceClass,
          })
          .ToList(),

        PowerSourceModules = db.PowerSourceModules
          .OrderBy(device => device.NumberChassis)
          .ThenBy(device => device.Number)
          .Select(device => new PowerSourceModuleConfigurationItem
          {
            Name = device.Name,
            Description = device.Description,
            Number = device.Number,
            NumberChassis = device.NumberChassis,
            ConnectionDetails = device.ConnectionDetails,
            DeviceClass = device.DeviceClass,
            ResistanceCalibrationJson = device.ResistanceCalibrationJson,
          })
          .ToList(),

        FastMeters = db.FastMeters
          .OrderBy(device => device.NumberChassis)
          .ThenBy(device => device.Number)
          .Select(device => new FastMeterConfigurationItem
          {
            Name = device.Name,
            Description = device.Description,
            Number = device.Number,
            NumberChassis = device.NumberChassis,
            ConnectionDetails = device.ConnectionDetails,
            DeviceClass = device.DeviceClass,
            MaxContinuityResistance = device.MaxContinuityResistance,
          })
          .ToList(),

        BreakdownTesters = db.BreakdownTesters
          .OrderBy(device => device.NumberChassis)
          .ThenBy(device => device.Number)
          .Select(device => new BreakdownTesterConfigurationItem
          {
            Name = device.Name,
            Description = device.Description,
            Number = device.Number,
            NumberChassis = device.NumberChassis,
            ConnectionDetails = device.ConnectionDetails,
            DeviceClass = device.DeviceClass,
            PiMaxVoltage = device.PiMaxVoltage,
            SiMaxVoltage = device.SiMaxVoltage,
            IRMinVoltage = device.IRMinVoltage,
          })
          .ToList(),
      };
    }

    private static DeviceConfigurationFileModel ParseConfigurationFile(string json)
    {
      var model = JsonSerializer.Deserialize<DeviceConfigurationFileModel>(json, ImportJsonOptions)
        ?? throw new InvalidDataException("Не удалось прочитать JSON конфигурации.");

      model.Chassis ??= new List<ChassisConfigurationItem>();
      model.Racks ??= new List<RackDto>();
      model.RelaySwitchModules ??= new List<RelaySwitchModuleConfigurationItem>();
      model.SwitchingDevices ??= new List<SwitchingDeviceConfigurationItem>();
      model.PowerSourceModules ??= new List<PowerSourceModuleConfigurationItem>();
      model.FastMeters ??= new List<FastMeterConfigurationItem>();
      model.BreakdownTesters ??= new List<BreakdownTesterConfigurationItem>();

      return model;
    }

    private static void ValidateImportedConfiguration(DeviceConfigurationFileModel model)
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

    private static void ApplyConfigurationFile(DeviceConfigurationFileModel model)
    {
      using var db = DataBaseConfig.Context;
      using var transaction = db.Database.BeginTransaction();

      db.BreakdownTesters.RemoveRange(db.BreakdownTesters);
      db.FastMeters.RemoveRange(db.FastMeters);
      db.PowerSourceModules.RemoveRange(db.PowerSourceModules);
      db.RelaySwitchModules.RemoveRange(db.RelaySwitchModules);
      db.SwitchingDevices.RemoveRange(db.SwitchingDevices);
      Racks.DeleteAllAsync().GetAwaiter().GetResult();

      db.ChassisManagers.RemoveRange(db.ChassisManagers);
      db.SaveChanges();

      db.ChassisManagers.AddRange(model.Chassis.Select(ToEntity));
      Racks.CreateRangeAsync(model.Racks.Select(ToEntity));

      db.RelaySwitchModules.AddRange(model.RelaySwitchModules.Select(ToEntity));
      db.SwitchingDevices.AddRange(model.SwitchingDevices.Select(ToEntity));
      db.PowerSourceModules.AddRange(model.PowerSourceModules.Select(ToEntity));
      db.FastMeters.AddRange(model.FastMeters.Select(ToEntity));
      db.BreakdownTesters.AddRange(model.BreakdownTesters.Select(ToEntity));
      db.SaveChanges();

      transaction.Commit();
      ReloadDeviceCaches();
    }

    private static void ReloadDeviceCaches()
    {
      DeviceRuntime.ClearCache();
    }

    private static string NormalizeRequired(string? value)
    {
      return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
      return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ChassisManagerEntity ToEntity(ChassisConfigurationItem item)
    {
      return new ChassisManagerEntity
      {
        Name = NormalizeRequired(item.Name),
        Description = NormalizeRequired(item.Description),
        Number = item.Number,
        ConnectionDetails = NormalizeRequired(item.ConnectionDetails),
        DeviceClass = NormalizeRequired(item.DeviceClass),
        BusType = item.BusType,
      };
    }

    private static IRack ToEntity(RackDto item) => Racks.Build(item);

    private static RelaySwitchModuleEntity ToEntity(RelaySwitchModuleConfigurationItem item)
    {
      return new RelaySwitchModuleEntity
      {
        Name = NormalizeRequired(item.Name),
        Description = NormalizeRequired(item.Description),
        Number = item.Number,
        NumberChassis = item.NumberChassis,
        NumberRack = item.NumberRack,
        PointCount = item.PointCount,
        ConnectionDetails = NormalizeRequired(item.ConnectionDetails),
        DeviceClass = NormalizeRequired(item.DeviceClass),
        SwitchResistance = item.SwitchResistance,
        SwitchCapacitance = item.SwitchCapacitance,
        BusType = item.BusType,
      };
    }

    private static SwitchingDeviceEntity ToEntity(SwitchingDeviceConfigurationItem item)
    {
      return new SwitchingDeviceEntity
      {
        Name = NormalizeRequired(item.Name),
        Description = NormalizeRequired(item.Description),
        Number = item.Number,
        NumberChassis = item.NumberChassis,
        ConnectionDetails = NormalizeRequired(item.ConnectionDetails),
        DeviceClass = NormalizeRequired(item.DeviceClass),
      };
    }

    private static PowerSourceModuleEntity ToEntity(PowerSourceModuleConfigurationItem item)
    {
      return new PowerSourceModuleEntity
      {
        Name = NormalizeRequired(item.Name),
        Description = NormalizeRequired(item.Description),
        Number = item.Number,
        NumberChassis = item.NumberChassis,
        ConnectionDetails = NormalizeRequired(item.ConnectionDetails),
        DeviceClass = NormalizeRequired(item.DeviceClass),
        ResistanceCalibrationJson = NormalizeOptional(item.ResistanceCalibrationJson),
      };
    }

    private static FastMeterEntity ToEntity(FastMeterConfigurationItem item)
    {
      return new FastMeterEntity
      {
        Name = NormalizeRequired(item.Name),
        Description = NormalizeRequired(item.Description),
        Number = item.Number,
        NumberChassis = item.NumberChassis,
        ConnectionDetails = NormalizeRequired(item.ConnectionDetails),
        DeviceClass = NormalizeRequired(item.DeviceClass),
        MaxContinuityResistance = item.MaxContinuityResistance,
      };
    }

    private static BreakdownTesterEntity ToEntity(BreakdownTesterConfigurationItem item)
    {
      return new BreakdownTesterEntity
      {
        Name = NormalizeRequired(item.Name),
        Description = NormalizeRequired(item.Description),
        Number = item.Number,
        NumberChassis = item.NumberChassis,
        ConnectionDetails = NormalizeRequired(item.ConnectionDetails),
        DeviceClass = NormalizeRequired(item.DeviceClass),
        PiMaxVoltage = item.PiMaxVoltage,
        SiMaxVoltage = item.SiMaxVoltage,
        IRMinVoltage = item.IRMinVoltage,
      };
    }

    private static string BuildPrintableConfiguration(
      IReadOnlyCollection<IChassisManager> chassisList,
      Func<int, IEnumerable<IFastMeter>> fastMeterService,
      Func<int, IEnumerable<IBreakdownTester>> breakdownService,
      Func<int, IEnumerable<IPowerSourceModule>> powerSourceService,
      Func<int, IEnumerable<IRelaySwitchModule>> getRelaySwitchModules,
      Func<int, IEnumerable<ISwitchingDevice>> getSwitchingDevices)
    {
      if (chassisList.Count == 0)
      {
        return "Конфигурация устройств не заполнена.";
      }

      var sb = new StringBuilder();
      int chassisIndex = 1;

      foreach (var chassis in chassisList)
      {
        if (sb.Length > 0)
        {
          sb.AppendLine(new string('=', 70));
        }

        sb.AppendLine($"Шасси #{chassisIndex}");
        AppendField(sb, "Модель устройства", chassis.Name, 2);
        AppendField(sb, "Номер шасси", chassis.Number, 2);
        AppendConnectionDetails(sb, chassis.ConnectionDetails, 2);

        sb.AppendLine();
        sb.AppendLine("  Устройства:");

        int devicesPrinted = 0;

        devicesPrinted += AppendDeviceSection(
          sb,
          "Модуль коммутации релейный",
          getRelaySwitchModules(chassis.Number),
          insertSectionSeparator: devicesPrinted > 0,
          (builder, device) =>
          {
            AppendField(builder, "Тип структурной шины", device.BusType.ToString(), 4);
            AppendField(builder, "Сопротивление коммутатора, Ом", device.SwitchResistance, 4);
            AppendField(builder, "Ёмкость коммутатора, нФ", device.SwitchCapacitance, 4);
          });

        devicesPrinted += AppendDeviceSection(
          sb,
          "Устройство коммутации шин",
          getSwitchingDevices(chassis.Number),
          insertSectionSeparator: devicesPrinted > 0);

        devicesPrinted += AppendDeviceSection(
          sb,
          "Модуль ист. напряжения и тока",
          powerSourceService(chassis.Number),
          insertSectionSeparator: devicesPrinted > 0);

        devicesPrinted += AppendDeviceSection(
          sb,
          "Измеритель (быстрый)",
          fastMeterService(chassis.Number),
          insertSectionSeparator: devicesPrinted > 0);

        devicesPrinted += AppendDeviceSection(
          sb,
          "Пробойная установка",
          breakdownService(chassis.Number),
          insertSectionSeparator: devicesPrinted > 0);

        if (devicesPrinted == 0)
        {
          sb.AppendLine("    Не добавлено ни одного устройства.");
        }

        chassisIndex++;
      }

      return sb.ToString().TrimEnd();
    }

    private static int AppendDeviceSection<TDevice>(
      StringBuilder sb,
      string title,
      IEnumerable<TDevice> devices,
      bool insertSectionSeparator = false,
      Action<StringBuilder, TDevice>? appendAdditional = null)
      where TDevice : class, IDevice
    {
      var deviceList = devices
        .OrderBy(device => device.Number)
        .ToList();

      if (deviceList.Count == 0)
      {
        return 0;
      }

      if (insertSectionSeparator)
      {
        sb.AppendLine();
      }

      int currentIndex = 1;
      foreach (var device in deviceList)
      {
        string suffix = deviceList.Count > 1 ? $" #{currentIndex}" : string.Empty;
        sb.AppendLine($"    {title}{suffix}");

        AppendField(sb, "Модель устройства", device.Name, 4);
        AppendField(sb, "Номер устройства", device.Number, 4);
        AppendConnectionDetails(sb, device.ConnectionDetails, 4);

        appendAdditional?.Invoke(sb, device);

        currentIndex++;
        if (currentIndex <= deviceList.Count)
        {
          sb.AppendLine();
        }
      }

      return deviceList.Count;
    }

    private static void AppendConnectionDetails(StringBuilder sb, string? connectionDetails, int indent)
    {
      if (string.IsNullOrWhiteSpace(connectionDetails))
      {
        return;
      }

      if (TryGetIp(connectionDetails, out var ipAddress))
      {
        AppendField(sb, "Тип подключения устройства", "IP", indent);
        AppendField(sb, "IP Address", ipAddress, indent);
        return;
      }

      if (TryGetCom(connectionDetails, out var comSettings) && comSettings != null)
      {
        AppendField(sb, "Тип подключения устройства", "COM", indent);
        AppendField(sb, "COM-порт", comSettings.PortName, indent);
        AppendField(sb, "Бит в секунду", comSettings.BaudRate, indent);
        AppendField(sb, "Стоповые биты", GetStopBitsText(comSettings.StopBits), indent);
        AppendField(sb, "Биты данных", comSettings.DataBits, indent);
        AppendField(sb, "Чётность", GetParityText(comSettings.Parity), indent);
        return;
      }

      AppendField(sb, "Адрес подключения", connectionDetails, indent);
    }

    private static bool TryGetIp(string connectionDetails, out string ipAddress)
    {
      ipAddress = string.Empty;

      if (string.IsNullOrWhiteSpace(connectionDetails))
      {
        return false;
      }

      if (connectionDetails.Contains("{"))
      {
        return false;
      }

      string token = connectionDetails.Trim().Split(':')[0];
      if (!IPAddress.TryParse(token, out var parsed))
      {
        return false;
      }

      ipAddress = parsed.ToString();
      return true;
    }

    private static bool TryGetCom(string connectionDetails, out SerialPortCustom? comSettings)
    {
      comSettings = null;

      if (string.IsNullOrWhiteSpace(connectionDetails))
      {
        return false;
      }

      try
      {
        comSettings = SerialPortCustom.ToObject(connectionDetails);
        return comSettings != null;
      }
      catch
      {
        return false;
      }
    }

    private static string GetStopBitsText(StopBits stopBits)
    {
      return stopBits switch
      {
        StopBits.One => "1",
        StopBits.OnePointFive => "1.5",
        StopBits.Two => "2",
        _ => stopBits.ToString()
      };
    }

    private static string GetParityText(Parity parity)
    {
      return parity switch
      {
        Parity.Even => "Чет",
        Parity.Odd => "Нечет",
        Parity.Mark => "Маркер",
        Parity.Space => "Пробел",
        _ => "Нет"
      };
    }

    private static void AppendField(StringBuilder sb, string label, string? value, int indent)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return;
      }

      sb.Append(' ', indent);
      sb.Append(label);
      sb.Append(": ");
      sb.AppendLine(value);
    }

    private static void AppendField(StringBuilder sb, string label, int value, int indent)
    {
      if (value <= 0)
      {
        return;
      }

      AppendField(sb, label, value.ToString(CultureInfo.InvariantCulture), indent);
    }

    private static void AppendField(StringBuilder sb, string label, double value, int indent)
    {
      if (value <= 0)
      {
        return;
      }

      AppendField(sb, label, value.ToString("0.###", CultureInfo.InvariantCulture), indent);
    }

    /// <summary>
    /// Печатает форматированный текст через стандартное диалоговое окно принтера.
    /// </summary>
    private void PrintText(string text)
    {
      var paragraph = new Paragraph(new Run(text))
      {
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
        FontSize = 12,
        Margin = new Thickness(30, 20, 30, 20),
        TextAlignment = TextAlignment.Left
      };

      var document = new FlowDocument(paragraph)
      {
        PagePadding = new Thickness(50),
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
        FontSize = 12,
        ColumnWidth = double.PositiveInfinity,
        ColumnGap = 0,
        IsColumnWidthFlexible = false,
        TextAlignment = TextAlignment.Left
      };

      PrintDialog printDialog = new PrintDialog();
      if (printDialog.ShowDialog() == true)
      {
        IDocumentPaginatorSource idpSource = document;
        printDialog.PrintDocument(idpSource.DocumentPaginator, "Конфигурация устройств");
      }
    }

    private sealed class DeviceConfigurationFileModel
    {
      public int Version { get; set; }

      public DateTime ExportedAtUtc { get; set; }

      public List<ChassisConfigurationItem> Chassis { get; set; } = new();

      public List<RackDto> Racks { get; set; } = new();

      public List<RelaySwitchModuleConfigurationItem> RelaySwitchModules { get; set; } = new();

      public List<SwitchingDeviceConfigurationItem> SwitchingDevices { get; set; } = new();

      public List<PowerSourceModuleConfigurationItem> PowerSourceModules { get; set; } = new();

      public List<FastMeterConfigurationItem> FastMeters { get; set; } = new();

      public List<BreakdownTesterConfigurationItem> BreakdownTesters { get; set; } = new();
    }

    private sealed class ChassisConfigurationItem
    {
      public string? Name { get; set; }

      public string? Description { get; set; }

      public int Number { get; set; }

      public string? ConnectionDetails { get; set; }

      public string? DeviceClass { get; set; }

      public BusStructureEnum.Type BusType { get; set; }
    }

    private sealed class RelaySwitchModuleConfigurationItem
    {
      public string? Name { get; set; }

      public string? Description { get; set; }

      public int Number { get; set; }

      public int NumberChassis { get; set; }

      public int NumberRack { get; set; }

      public int PointCount { get; set; }

      public string? ConnectionDetails { get; set; }

      public string? DeviceClass { get; set; }

      public double SwitchResistance { get; set; }

      public double SwitchCapacitance { get; set; }

      public SwitchingBusNew BusType { get; set; }
    }

    private sealed class SwitchingDeviceConfigurationItem
    {
      public string? Name { get; set; }

      public string? Description { get; set; }

      public int Number { get; set; }

      public int NumberChassis { get; set; }

      public string? ConnectionDetails { get; set; }

      public string? DeviceClass { get; set; }
    }

    private sealed class PowerSourceModuleConfigurationItem
    {
      public string? Name { get; set; }

      public string? Description { get; set; }

      public int Number { get; set; }

      public int NumberChassis { get; set; }

      public string? ConnectionDetails { get; set; }

      public string? DeviceClass { get; set; }

      public string? ResistanceCalibrationJson { get; set; }
    }

    private sealed class FastMeterConfigurationItem
    {
      public string? Name { get; set; }

      public string? Description { get; set; }

      public int Number { get; set; }

      public int NumberChassis { get; set; }

      public string? ConnectionDetails { get; set; }

      public string? DeviceClass { get; set; }

      public int MaxContinuityResistance { get; set; }
    }

    private sealed class BreakdownTesterConfigurationItem
    {
      public string? Name { get; set; }

      public string? Description { get; set; }

      public int Number { get; set; }

      public int NumberChassis { get; set; }

      public string? ConnectionDetails { get; set; }

      public string? DeviceClass { get; set; }

      public int PiMaxVoltage { get; set; }

      public int SiMaxVoltage { get; set; }

      public int IRMinVoltage { get; set; }
    }
  }
}
