using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands
{
  /// <summary>
  /// Профиль команд измерения ёмкости.
  /// </summary>
  public class CapacitanceMeasurementProfile : IMeasurementProfile
  {
    /// <summary>
    /// Режим работы мультиметра.
    /// </summary>
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.Capacitance;

    /// <summary>
    /// Тип выполняемого электрического испытания.
    /// </summary>
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.Capacitance;

    /// <summary>
    /// Единица измерения результата.
    /// </summary>
    public Enum Unit => CapacitanceUnit.NanoFarad;

    /// <summary>
    /// Команда перевода мультиметра в режим измерения ёмкости.
    /// </summary>
    public string SetMode { get; init; } = "CONF:CAP";

    /// <summary>
    /// Ожидаемое значение режима, возвращаемое устройством.
    /// </summary>
    public string CheckMode { get; init; } = "CAP";

    /// <summary>
    /// Команда получения текущего режима работы мультиметра.
    /// </summary>
    public string GetMode { get; init; } = "FUNC?";

    /// <summary>
    /// Команда выполнения измерения ёмкости.
    /// </summary>
    public string Measure { get; init; } = "MEAS:CAP?";

    /// <summary>
    /// Команда установки ручного диапазона измерения ёмкости.
    /// Плейсхолдер {0} заменяется диапазоном, {1} - рекомендуемым разрешением.
    /// </summary>
    public string SetRange { get; init; } = "CAP:RANG {0}";

    /// <summary>
    /// Команда включения автоматического выбора диапазона измерения ёмкости.
    /// </summary>
    public string SetAutoRange { get; init; } = "CAP:RANG:AUTO ON";

    /// <summary>
    /// Команда чтения последней ошибки прибора после установки диапазона.
    /// Если не задана, проверка ошибки не выполняется.
    /// </summary>
    public string? GetRangeError { get; init; }

    /// <summary>
    /// Поддерживаемые диапазоны измерения ёмкости в нФ.
    /// Если список пуст, используется переданное пользователем значение.
    /// </summary>
    public double[] SupportedRanges { get; init; } = Array.Empty<double>();

    /// <summary>
    /// Множитель преобразования диапазона из единицы профиля в единицу команды прибора.
    /// Значения ёмкости в приложении задаются в нФ, а SCPI-команды ожидают Ф.
    /// </summary>
    public double RangeCommandMultiplier { get; init; } = 1e-9d;

    /// <summary>
    /// Время ожидания ответа устройства, в миллисекундах.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
