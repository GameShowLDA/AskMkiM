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
  /// Класс для работы с режимом ACW (переменный ток высокого напряжения).
  /// </summary>
  public class AcwMode : IAcwModeBreakdown
  {
    /// <summary>
    /// Экземпляр модели устройства <see cref="GPT79904"/>, 
    /// используемый для выполнения команд и обмена данными с физическим прибором.
    /// </summary>
    private GPT79904 _gptModel { get; set; }

    /// <summary>
    /// Задержка (в миллисекундах) перед выполнением вызова команды.
    /// Применяется в методах, где требуется временной интервал для корректной реакции устройства.
    /// </summary>
    private static int delayBeforeCall = 100;

    /// <summary>
    /// Базовое значение задержки (в миллисекундах), используемое при выполнении команд к устройству.
    /// Определяет временной интервал ожидания между последовательными запросами.
    /// </summary>
    private int delay = 50;

    /// <summary>
    /// Текущая конфигурация режима ACW (переменный ток высокого напряжения),
    /// содержащая параметры, считанные с устройства или установленные пользователем.
    /// </summary>
    private AcwConfiguration _config;

    /// <inheritdoc />
    public IVoltageConfigurable Voltage { get; set; }

    /// <inheritdoc />
    public IModeConfigurable Mode { get; set; }

    /// <inheritdoc />
    public ICurrentLimitsConfigurable CurrentLimits { get; set; }

    /// <inheritdoc />
    public ITimeConfigurable Time { get; set; }

    /// <inheritdoc />
    public IOffsetConfigurable Offset { get; set; }

    /// <inheritdoc />
    public IArcCurrentConfigurable ArcCurrent { get; set; }

    /// <inheritdoc />
    public IFrequencyConfigurable FrequencyConfigurable { get; set; }

    /// <inheritdoc />
    public IMeasurable Measure { get; set; }

    /// <inheritdoc />
    public IConfigurationProvider<AcwConfiguration> Config { get; set; }

    /// <summary>
    /// Создает новый экземпляр класса <see cref="AcwMode"/>.
    /// </summary>
    /// <param name="gpt79904">Объект устройства GPT-79904.</param>
    public AcwMode(GPT79904 gpt79904)
    {
      _gptModel = gpt79904;
      _config = new AcwConfiguration();
      Voltage = new VoltageManagment(_gptModel, BreakdownTypeMode.ACW, delay, getConfigVoltage: () => _config.Voltage, setConfigVoltage: v => _config.Voltage = v);
      CurrentLimits = new CurrentLimitManagment(_gptModel, BreakdownTypeMode.ACW, delay, getHighLimit: () => _config.HighCurrentLimit, setHighLimit: v => _config.HighCurrentLimit = v, getLowLimit: () => _config.LowCurrentLimit, setLowLimit: v => _config.LowCurrentLimit = v);
      Time = new TimeManagment(_gptModel, BreakdownTypeMode.ACW, delay, getTestTime: () => _config.TestTime, setTestTime: v => _config.TestTime = v, getRampTime: () => _config.RampTime, setRampTime: v => _config.RampTime = v);
      Offset = new OffsetManagment(_gptModel, BreakdownTypeMode.ACW, delay, getOffset: () => _config.Offset, setOffset: v => _config.Offset = v);
      ArcCurrent = new ArcCurrentManagment(_gptModel, BreakdownTypeMode.ACW, delay, getArcCurrent: () => _config.ArcCurrent, setArcCurrent: v => _config.ArcCurrent = v);
      FrequencyConfigurable = new FrequencyManagment(_gptModel, BreakdownTypeMode.ACW, delay, getFrequency: () => _config.Frequency, setFrequency: v => _config.Frequency = v);
      Measure = new MeasureManagment(_gptModel, delayBeforeCall, getTestTime: async () => await Time.GetTestTimeAsync(), getRampTime: async () => await Time.GetRampTimeAsync(), getIsIdleMode: async () => await ExecutionConfig.GetIsIdleModeEnabled());
      Config = new AcwConfigManager(Voltage, CurrentLimits, Time, Offset, ArcCurrent, FrequencyConfigurable);
      Mode = new ModeManagment(_gptModel, BreakdownTypeMode.ACW, delay, async () => _config = await Config.ReadConfigurationAsync());
    }
  }
}
