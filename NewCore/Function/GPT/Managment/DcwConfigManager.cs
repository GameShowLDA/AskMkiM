using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using System.Text;

namespace NewCore.Function.GPT.Managment
{
  /// <summary>
  /// Менеджер конфигурации для режима DCW (постоянный ток высокого напряжения).
  /// Реализует интерфейс <see cref="IConfigurationProvider{T}"/>.
  /// </summary>
  internal class DcwConfigManager : IConfigurationProvider<DcwConfiguration>
  {
    private readonly IVoltageConfigurable _voltage;
    private readonly ICurrentLimitsConfigurable _currentLimits;
    private readonly ITimeConfigurable _time;
    private readonly IOffsetConfigurable _offset;
    private readonly IArcCurrentConfigurable _arcCurrent;

    private DcwConfiguration _config = new DcwConfiguration();

    /// <summary>
    /// Создаёт новый экземпляр <see cref="DcwConfigManager"/>.
    /// </summary>
    public DcwConfigManager(
      IVoltageConfigurable voltage,
      ICurrentLimitsConfigurable currentLimits,
      ITimeConfigurable time,
      IOffsetConfigurable offset,
      IArcCurrentConfigurable arcCurrent)
    {
      _voltage = voltage;
      _currentLimits = currentLimits;
      _time = time;
      _offset = offset;
      _arcCurrent = arcCurrent;
    }

    public async Task<string> GetConfigurationAsTextAsync()
    {
      var config = _config ?? await ReadConfigurationAsync();

      var sb = new StringBuilder();

      sb.AppendLine("Установленный режим: DCW");
      sb.AppendLine($"Напряжение:               {config.Voltage} кВ");
      sb.AppendLine($"Верхний предел тока:      {config.HighCurrentLimit} мА");
      sb.AppendLine($"Нижний предел тока:       {config.LowCurrentLimit} мА");
      sb.AppendLine($"Время теста:              {config.TestTime} с");
      sb.AppendLine($"Время нарастания:         {config.RampTime} с");
      sb.AppendLine($"Смещение (Offset):        {config.Offset} мА");
      sb.AppendLine($"Ток дугового пробоя:      {config.ArcCurrent} мА");

      return sb.ToString();
    }


    /// <inheritdoc />
    public async Task<DcwConfiguration> ReadConfigurationAsync()
    {
      _config = new DcwConfiguration
      {
        Voltage = await _voltage.GetVoltageAsync(),
        HighCurrentLimit = await _currentLimits.GetHighCurrentLimitAsync(),
        LowCurrentLimit = await _currentLimits.GetLowCurrentLimitAsync(),
        TestTime = await _time.GetTestTimeAsync(),
        RampTime = await _time.GetRampTimeAsync(),
        Offset = await _offset.GetOffsetAsync(),
        ArcCurrent = await _arcCurrent.GetArcCurrentAsync()
      };

      return _config;
    }

    /// <inheritdoc />
    public void ResetConfiguration()
    {
      _config = new DcwConfiguration();
    }
  }
}
