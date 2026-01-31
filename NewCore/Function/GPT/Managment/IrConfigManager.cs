using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using System.Text;

namespace NewCore.Function.GPT.Managment
{
  /// <summary>
  /// Менеджер конфигурации для режима IR (измерение сопротивления изоляции).
  /// Реализует интерфейс <see cref="IConfigurationProvider{T}"/>.
  /// </summary>
  internal class IrConfigManager : IConfigurationProvider<IrConfiguration>
  {
    private readonly IVoltageConfigurable _voltage;
    private readonly ITimeConfigurable _time;
    private readonly IOffsetConfigurable _offset;
    private readonly Func<Task<double>> _getHighResistanceLimit;
    private readonly Func<Task<double>> _getLowResistanceLimit;

    private IrConfiguration _config = new IrConfiguration();

    /// <summary>
    /// Создаёт новый экземпляр <see cref="IrConfigManager"/>.
    /// </summary>
    /// <param name="voltage">Компонент управления напряжением.</param>
    /// <param name="time">Компонент управления временем теста и нарастания.</param>
    /// <param name="offset">Компонент управления смещением (Offset).</param>
    /// <param name="getHighResistanceLimit">Функция получения верхнего предела сопротивления.</param>
    /// <param name="getLowResistanceLimit">Функция получения нижнего предела сопротивления.</param>
    public IrConfigManager(
      IVoltageConfigurable voltage,
      ITimeConfigurable time,
      IOffsetConfigurable offset,
      Func<Task<double>> getHighResistanceLimit,
      Func<Task<double>> getLowResistanceLimit)
    {
      _voltage = voltage;
      _time = time;
      _offset = offset;
      _getHighResistanceLimit = getHighResistanceLimit;
      _getLowResistanceLimit = getLowResistanceLimit;
    }

    public async Task<string> GetConfigurationAsTextAsync()
    {
      var config = _config ?? await ReadConfigurationAsync();

      var sb = new StringBuilder();

      sb.AppendLine("Установленный режим: IR");
      sb.AppendLine($"Напряжение:               {config.Voltage} В");
      sb.AppendLine($"Верхний предел сопротивления: {config.HighResistanceLimit} ГОм");
      sb.AppendLine($"Нижний предел сопротивления:  {config.LowResistanceLimit} ГОм");
      sb.AppendLine($"Время теста:              {config.TestTime} с");
      sb.AppendLine($"Время нарастания:         {config.RampTime} с");
      sb.AppendLine($"Смещение (Offset):        {config.Offset} ГОм");

      return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<IrConfiguration> ReadConfigurationAsync()
    {
      _config = new IrConfiguration
      {
        Voltage = await _voltage.GetVoltageAsync(),
        HighResistanceLimit = await _getHighResistanceLimit(),
        LowResistanceLimit = await _getLowResistanceLimit(),
        TestTime = await _time.GetTestTimeAsync(),
        Offset = await _offset.GetOffsetAsync(),
        RampTime = await _time.GetRampTimeAsync()
      };

      return _config;
    }

    /// <inheritdoc />
    public void ResetConfiguration()
    {
      _config = new IrConfiguration();
    }
  }
}
