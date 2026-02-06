using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using System.Text;

namespace NewCore.Function.GPT.Managment
{
  /// <summary>
  /// Менеджер конфигурации для режима ACW (переменный ток высокого напряжения).
  /// Реализует интерфейс <see cref="IConfigurationProvider{T}"/>.
  /// </summary>
  internal class AcwConfigManager : IConfigurationProvider<AcwConfiguration>
  {
    private readonly IVoltageConfigurable _voltage;
    private readonly ICurrentLimitsConfigurable _currentLimits;
    private readonly ITimeConfigurable _time;
    private readonly IOffsetConfigurable _offset;
    private readonly IArcCurrentConfigurable _arcCurrent;
    private readonly IFrequencyConfigurable _frequency;

    private AcwConfiguration _config = new AcwConfiguration();

    /// <summary>
    /// Создаёт новый экземпляр <see cref="AcwConfigManager"/>.
    /// </summary>
    public AcwConfigManager(
      IVoltageConfigurable voltage,
      ICurrentLimitsConfigurable currentLimits,
      ITimeConfigurable time,
      IOffsetConfigurable offset,
      IArcCurrentConfigurable arcCurrent,
      IFrequencyConfigurable frequency)
    {
      _voltage = voltage;
      _currentLimits = currentLimits;
      _time = time;
      _offset = offset;
      _arcCurrent = arcCurrent;
      _frequency = frequency;
    }

    public async Task<string> GetConfigurationAsTextAsync()
    {
      var config = await ReadConfigurationAsync();

      var sb = new StringBuilder();

      sb.AppendLine("Режим испытания: ACW");
      sb.AppendLine($"Напряжение: {config.Voltage} кВ");
      sb.AppendLine($"Верхний предел тока: {config.HighCurrentLimit} мА");
      sb.AppendLine($"Нижний предел тока: {config.LowCurrentLimit} мА");
      sb.AppendLine($"Время теста: {config.TestTime} с");
      sb.AppendLine($"Время нарастания: {config.RampTime} с");
      sb.AppendLine($"Частота: {config.Frequency} Гц");
      sb.AppendLine($"Смещение (Offset): {config.Offset} мА");
      sb.AppendLine($"Ток дугового пробоя: {config.ArcCurrent} мА");

      return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<AcwConfiguration> ReadConfigurationAsync()
    {
      _config = new AcwConfiguration
      {
        Voltage = await _voltage.GetVoltageAsync(),
        HighCurrentLimit = await _currentLimits.GetHighCurrentLimitAsync(),
        LowCurrentLimit = await _currentLimits.GetLowCurrentLimitAsync(),
        TestTime = await _time.GetTestTimeAsync(),
        Frequency = await _frequency.GetFrequencyAsync(),
        Offset = await _offset.GetOffsetAsync(),
        ArcCurrent = await _arcCurrent.GetArcCurrentAsync(),
        RampTime = await _time.GetRampTimeAsync()
      };

      return _config;
    }

    /// <inheritdoc />
    public void ResetConfiguration()
    {
      _config = new AcwConfiguration();
    }
  }
}
