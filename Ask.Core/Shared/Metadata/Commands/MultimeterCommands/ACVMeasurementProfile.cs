using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands
{
  /// <summary>
  /// Профиль команд измерения переменного напряжения.
  /// </summary>
  public class ACVMeasurementProfile : IMeasurementProfile
  {
    /// <summary>
    /// Режим работы мультиметра.
    /// </summary>
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.AcVoltage;

    /// <summary>
    /// Тип выполняемого электрического испытания.
    /// </summary>
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.ACVoltage;

    /// <summary>
    /// Единица измерения результата.
    /// </summary>
    public Enum Unit => VoltageUnit.Volt;

    /// <summary>
    /// Команда перевода мультиметра в режим измерения переменного напряжения.
    /// </summary>
    public string SetMode { get; init; } = "CONF:VOLT:AC";

    /// <summary>
    /// Ожидаемое значение режима, возвращаемое устройством.
    /// </summary>
    public string CheckMode { get; init; } = "VOLT:AC";

    /// <summary>
    /// Команда получения текущего режима работы мультиметра.
    /// </summary>
    public string GetMode { get; init; } = "FUNC?";

    /// <summary>
    /// Команда выполнения измерения переменного напряжения.
    /// </summary>
    public string Measure { get; init; } = "MEAS:VOLT:AC?";

    /// <summary>
    /// Команда установки ручного диапазона измерения переменного напряжения.
    /// Плейсхолдер {0} заменяется диапазоном, {1} - рекомендуемым разрешением.
    /// </summary>
    public string SetRange { get; init; } = "VOLT:AC:RANG {0}";

    /// <summary>
    /// Команда включения автоматического выбора диапазона измерения переменного напряжения.
    /// </summary>
    public string SetAutoRange { get; init; } = "VOLT:AC:RANG:AUTO ON";

    /// <summary>
    /// Команда чтения последней ошибки прибора после установки диапазона.
    /// Если не задана, проверка ошибки не выполняется.
    /// </summary>
    public string? GetRangeError { get; init; }

    /// <summary>
    /// Поддерживаемые диапазоны измерения переменного напряжения.
    /// Если список пуст, используется переданное пользователем значение.
    /// </summary>
    public double[] SupportedRanges { get; init; } = Array.Empty<double>();

    /// <summary>
    /// Время ожидания ответа устройства, в миллисекундах.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
