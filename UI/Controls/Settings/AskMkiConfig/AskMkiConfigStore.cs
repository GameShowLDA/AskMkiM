using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.DataBase.Provider.Services.Devices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UI.Controls.Settings.AskMkiConfig;

/// <summary>
/// Загрузка и сохранение профиля АСК в базе.
/// </summary>
public partial class AskMkiConfigControl
{
  /// <summary>
  /// Сохраняет профиль для текущей legacy-стойки АСК.
  /// </summary>
  private async Task SaveProfileToDatabaseAsync(LegacyMkiHardwareProfile profile)
  {
    var numberChassis = await TryResolveLegacyAskChassisNumberAsync();
    if (numberChassis == null)
    {
      return;
    }

    var service = new LegacyMkiHardwareProfileDtoService();
    await service.SaveProfileAsync(numberChassis.Value, _selectedProfileKind, profile);
    await SyncFastMeterAsync(numberChassis.Value, profile);
  }

  /// <summary>
  /// Сохраняет цифровой вольтметр АСК как обычный быстрый измеритель стойки.
  /// </summary>
  private static async Task SyncFastMeterAsync(int numberChassis, LegacyMkiHardwareProfile profile)
  {
    if (string.IsNullOrWhiteSpace(profile.HardwareAux.VoltmeterDeviceClass))
    {
      return;
    }

    var existing = (await FastMeters.GetDevicesByNumberChassisAsync(numberChassis))
      .FirstOrDefault();

    var dto = new FastMeterDto
    {
      Id = existing?.Id ?? 0,
      NumberChassis = numberChassis,
      Number = existing?.Number > 0 ? existing.Number : 1,
      Name = existing?.Name ?? "Цифровой вольтметр АСК",
      Description = existing?.Description ?? "Цифровой вольтметр стойки АСК.",
      DeviceType = DeviceType.FastMeter,
      DeviceClass = profile.HardwareAux.VoltmeterDeviceClass,
      ConnectionDetails = ResolveVoltmeterConnection(profile),
      TypeMode = MultimeterTypeMode.None,
      MaxContinuityResistance = existing?.MaxContinuityResistance > 0 ? existing.MaxContinuityResistance : 100000,
      AcwPpuDividerCoefficientPercent = existing?.AcwPpuDividerCoefficientPercent > 0 ? existing.AcwPpuDividerCoefficientPercent : 100d,
      DcwPpuDividerCoefficientPercent = existing?.DcwPpuDividerCoefficientPercent > 0 ? existing.DcwPpuDividerCoefficientPercent : 100d
    };

    var meter = FastMeters.Build(dto);

    if (existing == null)
    {
      await FastMeters.CreateAsync(meter);
      return;
    }

    await FastMeters.UpdateAsync(meter);
  }

  /// <summary>
  /// Возвращает строку подключения цифрового вольтметра из профиля АСК.
  /// </summary>
  private static string ResolveVoltmeterConnection(LegacyMkiHardwareProfile profile)
  {
    if (IsIpVoltmeter(profile.HardwareAux.VoltmeterDeviceClass) && !IsUsbVoltmeter(profile.HardwareAux.VoltmeterDeviceClass))
    {
      return profile.HardwareAux.VoltmeterIpAddress?.Trim() ?? string.Empty;
    }

    if (IsUsbVoltmeter(profile.HardwareAux.VoltmeterDeviceClass) && !IsIpVoltmeter(profile.HardwareAux.VoltmeterDeviceClass))
    {
      return profile.HardwareAux.UsbAddrVm?.Trim() ?? string.Empty;
    }

    if (string.Equals(profile.HardwareAux.VoltmeterConnectionType, "IP", StringComparison.OrdinalIgnoreCase))
    {
      return profile.HardwareAux.VoltmeterIpAddress?.Trim() ?? string.Empty;
    }

    return profile.HardwareAux.UsbAddrVm?.Trim() ?? string.Empty;
  }

  /// <summary>
  /// Подставляет в рабочий файл профиль, сохраненный в базе данных для текущей стойки.
  /// </summary>
  private void ApplyProfileFromDatabaseIfExists()
  {
    if (_loadedConfigFile == null)
    {
      return;
    }

    var numberChassis = TryResolveLegacyAskChassisNumberAsync()
      .GetAwaiter()
      .GetResult();

    if (numberChassis == null)
    {
      return;
    }

    var service = new LegacyMkiHardwareProfileDtoService();
    service.EnsureDefaultProfilesAsync(numberChassis.Value)
      .GetAwaiter()
      .GetResult();

    var profileDto = service.GetByChassisAsync(numberChassis.Value, _selectedProfileKind)
      .GetAwaiter()
      .GetResult();

    if (profileDto == null)
    {
      return;
    }

    _loadedConfigFile.SetProfile(_selectedProfileKind, profileDto.ToProfile());
  }

  /// <summary>
  /// Возвращает номер выбранной legacy-стойки АСК или находит первую такую стойку в базе.
  /// </summary>
  private async Task<int?> TryResolveLegacyAskChassisNumberAsync()
  {
    if (_numberChassis != null)
    {
      return _numberChassis.Value;
    }

    var service = new ChassisManagerDtoService();
    var chassis = await service.GetAllAsync();
    var legacyAsk = chassis.FirstOrDefault(x =>
      string.Equals(x.Name, "\u0422\u0435\u0441\u0442\u0435\u0440 \u0410\u0421\u041a", StringComparison.OrdinalIgnoreCase) ||
      x.DeviceClass.EndsWith(".ManagerASKMKI", StringComparison.Ordinal));

    return legacyAsk?.Number;
  }
}
