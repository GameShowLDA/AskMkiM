using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using UI.Controls.Settings.Configuration;

namespace MainWindowProgram.Init;

/// <summary>
/// Применяет стандартные настройки приложения и создаёт начальную конфигурацию оборудования.
/// </summary>
internal sealed class ApplicationAutoConfigurationService
{
  /// <summary>
  /// Применяет стандартные настройки приложения по отдельным разделам.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения стандартных настроек.</returns>
  public async Task ApplyDefaultConfigurationAsync(CancellationToken cancellationToken = default)
  {
    await ApplyDefaultEquipmentConfigurationAsync(cancellationToken);
    await ApplyDefaultExecutionSettingsAsync(cancellationToken);
    await ApplyDefaultProtocolSettingsAsync(cancellationToken);
    await ApplyDefaultInterfaceSettingsAsync(cancellationToken);
  }

  /// <summary>
  /// Применяет стандартную конфигурацию оборудования.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения конфигурации оборудования.</returns>
  /// <remarks>
  /// Конфигурация берётся из эталонной поставочной базы данных и сохраняется
  /// в текущую базу приложения одной транзакцией.
  /// </remarks>
  private static async Task ApplyDefaultEquipmentConfigurationAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var configuration = new DeviceConfigurationFileModel
    {
      Version = 1,
      ExportedAtUtc = DateTime.UtcNow,
      Chassis =
      [
        new ChassisManagerDto
        {
          Id = 1,
          BusType = BusStructureEnum.Type.Bus2,
          Name = "Тестер АСКМ",
          Description = "Добавить описание сюда",
          Number = 1,
          ConnectionDetails = "192.168.1.0",
          DeviceType = DeviceType.ChassisManager,
          DeviceClass = "Ask.Device.Runtime.Device.ManagerChassis"
        }
      ],
      RelaySwitchModules =
      [
        CreateDefaultRelayModule(1, 4, "192.168.1.4"),
        CreateDefaultRelayModule(2, 6, "192.168.1.6"),
        CreateDefaultRelayModule(3, 8, "192.168.1.8")
      ],
      SwitchingDevices =
      [
        new SwitchingDeviceDto
        {
          Id = 1,
          Name = "Устройство УКШ",
          Description = "Реализовать описание в Ask.Device.Runtime.Device.DeviceBusCommutation",
          Number = 20,
          ConnectionDetails = "192.168.1.20",
          DeviceType = DeviceType.SwitchingDevice,
          DeviceClass = "Ask.Device.Runtime.Device.DeviceBusCommutation",
          NumberChassis = 1
        }
      ],
      FastMeters =
      [
        new FastMeterDto
        {
          Id = 1,
          TypeMode = MultimeterTypeMode.None,
          MaxContinuityResistance = 100000,
          Name = "Keysight 34465A",
          Description = "Реализовать описание в Ask.Device.Runtime.Device.KeysightDevice",
          Number = 16,
          ConnectionDetails = "192.168.1.16",
          DeviceType = DeviceType.FastMeter,
          DeviceClass = "Ask.Device.Runtime.Device.KeysightDevice",
          NumberChassis = 1,
          AcwPpuDividerCoefficientPercent = 48,
          DcwPpuDividerCoefficientPercent = 8
        }
      ],
      BreakdownTesters =
      [
        new BreakdownTesterDto
        {
          Id = 1,
          Mode = BreakdownTypeMode.None,
          DcwMaxVoltage = 1000,
          SiMaxVoltage = 0,
          IRMinVoltage = 0,
          Name = "GPT79904",
          Description = "Реализовать описание в Ask.Device.Runtime.Device.GPT79904",
          Number = 1,
          ConnectionDetails = "{\r\n  \"PortName\": \"COM1\",\r\n  \"BaudRate\": 115200,\r\n  \"Parity\": \"None\",\r\n  \"DataBits\": 8,\r\n  \"StopBits\": \"One\",\r\n  \"Handshake\": \"None\",\r\n  \"EncodingName\": \"us-ascii\"\r\n}",
          DeviceType = DeviceType.BreakdownTester,
          DeviceClass = "Ask.Device.Runtime.Device.GPT79904",
          NumberChassis = 1,
          AcwMaxVoltage = 700
        }
      ]
    };

    await DeviceConfigurationService.ApplyConfigurationFileAsync(configuration, cancellationToken);
  }

  /// <summary>
  /// Создаёт запись модуля МКР-350 с параметрами из эталонной конфигурации.
  /// </summary>
  /// <param name="id">Идентификатор записи модуля.</param>
  /// <param name="number">Системный номер модуля.</param>
  /// <param name="connectionDetails">IP-адрес модуля.</param>
  /// <returns>Заполненная модель модуля МКР-350.</returns>
  private static RelaySwitchModuleDto CreateDefaultRelayModule(int id, int number, string connectionDetails)
  {
    return new RelaySwitchModuleDto
    {
      Id = id,
      NumberRack = 0,
      PointCount = 350,
      BusType = SwitchingBusNew.AB1,
      SwitchResistance = 1.5,
      SwitchCapacitance = 1.5,
      Name = "Модуль МКР-350",
      Description = "Добавить описание сюда",
      Number = number,
      ConnectionDetails = connectionDetails,
      DeviceType = DeviceType.RelaySwitchModule,
      DeviceClass = "Ask.Device.Runtime.Device.ModuleRelayControl",
      NumberChassis = 1
    };
  }

  /// <summary>
  /// Применяет стандартные настройки выполнения.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения настроек выполнения.</returns>
  private static async Task ApplyDefaultExecutionSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var defaultSettings = new SettingsExecutionDto
    {
      IdleModeExecution = false,
      IsErrorSimulationMode = false,
      StepByStepMode = false,
      StopOnError = false,
      LegacyCompatibilityMode = false
    };

    await ExecutionConfig.SaveExecutionModel(defaultSettings);
  }

  /// <summary>
  /// Применяет стандартные настройки протокола.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения настроек протокола.</returns>
  private static async Task ApplyDefaultProtocolSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    // Берём уже загруженную модель, чтобы не затереть шаблоны протоколов пустыми строками.
    var defaultSettings = ProtocolConfig.GetProtocolModel();
    defaultSettings.ShowDeviceInfo = true;
    defaultSettings.ShowHeaderInfo = true;
    defaultSettings.AutoSaveProtocol = true;
    defaultSettings.AutoPrintProtocol = true;
    defaultSettings.DisplayOperationTime = true;
    defaultSettings.ShowDetailedProtocol = true;
    defaultSettings.ShowProtocolInSoftware = true;
    defaultSettings.GenerateProtocol = true;
    defaultSettings.ShowCommandHeadersInProtocol = true;
    defaultSettings.ShowTestStepMessagesInProtocol = true;

    await ProtocolConfig.SaveProtocolModel(defaultSettings);
  }

  /// <summary>
  /// Применяет стандартные настройки интерфейса пользователя.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Асинхронная задача применения настроек интерфейса пользователя.</returns>
  private static async Task ApplyDefaultInterfaceSettingsAsync(
    CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var defaultInterfaceSettings = await UserInterfaceConfig.GetParameterModel();
    defaultInterfaceSettings.Language = "ru";
    defaultInterfaceSettings.Theme = ThemeMode.DarkCustom;
    defaultInterfaceSettings.UseSyntaxHighlighting = true;
    defaultInterfaceSettings.UseCommandBodyBackgroundHighlighting = true;
    defaultInterfaceSettings.UseChainPointBodyBackgroundHighlighting = true;

    await UserInterfaceConfig.SaveProtocolModel(defaultInterfaceSettings);

    var defaultDeviceDisplaySettings = DeviceDisplayConfig.GetDeviceDisplayModel();
    defaultDeviceDisplaySettings.ShowMachineAddresses = true;
    defaultDeviceDisplaySettings.ShowConnectionInfo = true;
    defaultDeviceDisplaySettings.ShowDeviceExecutionParameters = true;
    defaultDeviceDisplaySettings.ShowMeasurementResults = true;
    defaultDeviceDisplaySettings.ShowIntermediateMeasurementResults = true;

    await DeviceDisplayConfig.SaveSettingsAsync(defaultDeviceDisplaySettings);

    var defaultProtocolSettings = ProtocolConfig.GetProtocolModel();
    defaultProtocolSettings.PrintFontFamily = "Consolas";
    defaultProtocolSettings.PrintFontSize = 16;

    await ProtocolConfig.SaveProtocolModel(defaultProtocolSettings);
  }
}
