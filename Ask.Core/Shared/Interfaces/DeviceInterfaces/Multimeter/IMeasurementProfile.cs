using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter
{
  /// <summary>
  /// Определяет профиль измерения мультиметра, содержащий команды и параметры,
  /// необходимые для переключения режима, проверки текущего режима и выполнения измерения.
  /// </summary>
  public interface IMeasurementProfile
  {
    MultimeterTypeMode TypeMode { get; }
    ElectricalTestFunction ElectricalTest { get; }

    Enum Unit { get; }

    /// <summary>
    /// Команда переключения мультиметра в режим измерения.
    /// </summary>
    string SetMode { get; init; }

    /// <summary>
    /// Ожидаемое значение, подтверждающее успешное переключение режима.
    /// </summary>
    string CheckMode { get; init; }

    /// <summary>
    /// Команда получения текущего режима работы мультиметра.
    /// </summary>
    string GetMode { get; init; }

    /// <summary>
    /// Команда выполнения измерения.
    /// </summary>
    string Measure { get; init; }

    /// <summary>
    /// Максимальное время ожидания ответа от прибора в миллисекундах.
    /// </summary>
    int Timeout { get; init; }
  }
}