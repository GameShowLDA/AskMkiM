using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands
{
  /// <summary>
  /// Профиль команд измерения электрического сопротивления.
  /// </summary>
  public class ResistanceMeasurementProfile : IMeasurementProfile
  {
    /// <summary>
    /// Режим работы мультиметра.
    /// </summary>
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.Resistance;

    /// <summary>
    /// Тип выполняемого электрического испытания.
    /// </summary>
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.Resistance;

    /// <summary>
    /// Единица измерения результата.
    /// </summary>
    public Enum Unit => ResistanceUnit.Ohm;

    /// <summary>
    /// Команда перевода мультиметра в режим измерения сопротивления.
    /// </summary>
    public string SetMode { get; init; } = "CONF:RES";

    /// <summary>
    /// Ожидаемое значение режима, возвращаемое устройством.
    /// </summary>
    public string CheckMode { get; init; } = "RES";

    /// <summary>
    /// Команда получения текущего режима работы мультиметра.
    /// </summary>
    public string GetMode { get; init; } = "FUNC?";

    /// <summary>
    /// Команда выполнения измерения сопротивления.
    /// </summary>
    public string Measure { get; init; } = "MEAS:RES?";

    /// <summary>
    /// Команда установки ручного диапазона измерения сопротивления.
    /// Плейсхолдер {0} заменяется диапазоном, {1} - рекомендуемым разрешением.
    /// </summary>
    public string SetRange { get; init; } = "RES:RANG {0}";

    /// <summary>
    /// Команда включения автоматического выбора диапазона измерения сопротивления.
    /// </summary>
    public string SetAutoRange { get; init; } = "RES:RANG:AUTO ON";

    /// <summary>
    /// Команда чтения последней ошибки прибора после установки диапазона.
    /// Если не задана, проверка ошибки не выполняется.
    /// </summary>
    public string? GetRangeError { get; init; }

    /// <summary>
    /// Поддерживаемые диапазоны измерения сопротивления.
    /// Если список пуст, используется переданное пользователем значение.
    /// </summary>
    public double[] SupportedRanges { get; init; } = Array.Empty<double>();

    /// <summary>
    /// Время ожидания ответа устройства, в миллисекундах.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
