using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;

namespace UI.Controls.Settings.Configuration;

/// <summary>
/// Модель файла конфигурации устройств для импорта, экспорта и печати.
/// </summary>
public sealed class DeviceConfigurationFileModel
{
  public int Version { get; set; }

  public DateTime ExportedAtUtc { get; set; }

  public List<ChassisManagerDto> Chassis { get; set; } = new();

  public List<RackDto> Racks { get; set; } = new();

  public List<RelaySwitchModuleDto> RelaySwitchModules { get; set; } = new();

  public List<SwitchingDeviceDto> SwitchingDevices { get; set; } = new();

  public List<PowerSourceModuleDto> PowerSourceModules { get; set; } = new();

  public List<FastMeterDto> FastMeters { get; set; } = new();

  public List<BreakdownTesterDto> BreakdownTesters { get; set; } = new();
}
