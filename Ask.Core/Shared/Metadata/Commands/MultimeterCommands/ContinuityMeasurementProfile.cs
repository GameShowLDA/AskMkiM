using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands
{
  /// <summary>
  /// Профиль команд режима прозвонки.
  /// </summary>
  public class ContinuityMeasurementProfile : IMeasurementProfile
  {
    /// <summary>
    /// Режим работы мультиметра.
    /// </summary>
    public MultimeterTypeMode TypeMode => MultimeterTypeMode.Continuity;

    /// <summary>
    /// Тип выполняемого электрического испытания.
    /// </summary>
    public ElectricalTestFunction ElectricalTest => ElectricalTestFunction.Continuity;

    /// <summary>
    /// Единица измерения результата.
    /// </summary>
    public Enum Unit => ResistanceUnit.Ohm;

    /// <summary>
    /// Команда перевода мультиметра в режим прозвонки.
    /// </summary>
    public string SetMode { get; init; } = "CONF:CONT";

    /// <summary>
    /// Ожидаемое значение режима, возвращаемое устройством.
    /// </summary>
    public string CheckMode { get; init; } = "CONT";

    /// <summary>
    /// Команда получения текущего режима работы мультиметра.
    /// </summary>
    public string GetMode { get; init; } = "FUNC?";

    /// <summary>
    /// Команда выполнения прозвонки.
    /// </summary>
    public string Measure { get; init; } = "MEAS:CONT?";

    /// <summary>
    /// Время ожидания ответа устройства, в миллисекундах.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}