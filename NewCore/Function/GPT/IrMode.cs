using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Device;
using NewCore.Function.GPT.Managment;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для управления режимом IR (Insulation Resistance).
  /// </summary>
  public class IrMode : IIrModeBreakdown
  {
    private GPT79904 _gptModel { get; set; }
    private static double timeDelay = 2;
    private static int delayBeforeCall = 100;
    private int delay = 100;
    private IrConfiguration _config;

    /// <inheritdoc />
    public IModeConfigurable Mode { get; set; }

    /// <inheritdoc />

    public IVoltageConfigurable Voltage { get; set; }

    /// <inheritdoc />

    public ITimeConfigurable Time { get; set; }

    /// <inheritdoc />
    public IOffsetConfigurable Offset { get; set; }

    /// <inheritdoc />
    public IMeasurable Measure { get; set; }

    /// <inheritdoc />
    public IConfigurationProvider<IrConfiguration> Config { get; set; }

    /// <inheritdoc />
    public IResistanceLimitsConfigurable ResistanceLimits { get; set; }

    public IrMode(GPT79904 gpt79904)
    {
      _gptModel = gpt79904;
      _config = new IrConfiguration();
      Voltage = new VoltageManagment(_gptModel, BreakdownTypeMode.IR, delay, getConfigVoltage: () => _config.Voltage, setConfigVoltage: v => _config.Voltage = v);
      Time = new TimeManagment(_gptModel, BreakdownTypeMode.IR, delay, getTestTime: () => _config.TestTime, setTestTime: v => _config.TestTime = v, getRampTime: () => _config.RampTime, setRampTime: v => _config.RampTime = v);
      Offset = new OffsetManagment(_gptModel, BreakdownTypeMode.IR, delay, getOffset: () => _config.Offset, setOffset: v => _config.Offset = v);
      Measure = new MeasureManagment(_gptModel, delayBeforeCall, getTestTime: () => Task.FromResult(_config.TestTime), getRampTime: () => Task.FromResult(_config.RampTime), getIsIdleMode: ExecutionConfig.GetIsIdleModeEnabled);
      ResistanceLimits = new ResistanceLimitsManagment(_gptModel, delay, ExecutionConfig.GetIsIdleModeEnabled, () => _config.HighResistanceLimit, v => _config.HighResistanceLimit = v, () => _config.LowResistanceLimit, v => _config.LowResistanceLimit = v);
      Config = new IrConfigManager(Voltage, Time, Offset, ResistanceLimits.GetHighResistanceLimitAsync, ResistanceLimits.GetLowResistanceLimitAsync);
      Mode = new ModeManagment(_gptModel, BreakdownTypeMode.IR, delay, async () => _config = await Config.ReadConfigurationAsync());
    }
  }
}
