using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Core.Services.Errors.Metrology
{
  /// <summary>
  /// Содержит стандартные исключения, возникающие при валидации метрологических данных —
  /// точек подключения, параметров времени, напряжения, шины и других входных данных.
  /// </summary>
  public static class MetrologyValidationErrors
  {
    /// <summary>
    /// Исключение: элемент ввода данных (InputField) не найден в пользовательском интерфейсе.
    /// </summary>
    public static SystemExceptionBase InputFieldNotFound() =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InputFieldNotFound,
        Description = "Элемент ввода не найден."
      });

    /// <summary>
    /// Исключение: указана некорректная первая точка подключения.
    /// </summary>
    public static SystemExceptionBase InvalidFirstPoint() =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InvalidFirstPointFormat,
        Description = "Неверный формат первой точки."
      });

    /// <summary>
    /// Исключение: указана некорректная вторая точка подключения.
    /// </summary>
    public static SystemExceptionBase InvalidSecondPoint() =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InvalidSecondPointFormat,
        Description = "Неверный формат второй точки."
      });

    /// <summary>
    /// Исключение: электрический параметр должен быть числовым значением.
    /// </summary>
    public static SystemExceptionBase InvalidElectricalValue() =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InvalidParameter,
        Description = "Не удалось распознать электрический параметр. Параметр должен быть целым или дробным числом(x.y)."
      });

    /// <summary>
    /// Исключение: указано некорректное значение напряжения.
    /// </summary>
    public static SystemExceptionBase InvalidVoltage() =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InvalidVoltage,
        Description = "Значение напряжения должно быть числом вида x.y."
      });

    /// <summary>
    /// Исключение: указано некорректное значение времени выполнения или измерения.
    /// </summary>
    public static SystemExceptionBase InvalidTime() =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InvalidTime,
        Description = "Не удалось распознать время. Параметр должен быть целым или дробным числом(x.y)."
      });

    /// <summary>
    /// Исключение: точка подключения выходит за пределы диапазона допустимых значений.
    /// </summary>
    public static SystemExceptionBase PointOutOfRange(int point, int maxPoint) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_PointOutOfRange,
        Description = $"Точка {point} выходит за предел допустимого диапазона (1–{maxPoint})."
      });

    /// <summary>
    /// Исключение: шасси с указанным номером не найдено в конфигурации.
    /// </summary>
    public static SystemExceptionBase ChassisNotFound(int chassisNumber) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InvalidFirstPointFormat,
        Description = $"Шасси с номером {chassisNumber} не найдено."
      });

    /// <summary>
    /// Исключение: модуль коммутации не найден в указанном шасси.
    /// </summary>
    public static SystemExceptionBase ModuleNotFound(int chassisNumber, int moduleNumber) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InvalidSecondPointFormat,
        Description = $"Модуль {moduleNumber} в шасси {chassisNumber} не найден."
      });

    /// <summary>
    /// Исключение: точки подключения не уникальны (повторяются).
    /// </summary>
    /// <param name="first">Первая точка.</param>
    /// <param name="second">Вторая точка.</param>
    public static SystemExceptionBase PointsNotUnique(string first, string second) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_PointsNotUnique,
        Description = $"Точки подключения {first} и {second} не уникальны. Проверьте правильность ввода."
      });

    /// <summary>
    /// Исключение: указана некорректная шина подключения.
    /// </summary>
    public static SystemExceptionBase InvalidBusSelection() =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_InvalidBus,
        Description = "Шина подключения указана некорректно. Проверьте выбранное значение."
      });

    /// <summary>
    /// Исключение: при сборе устройств возникла ошибка.
    /// Используется для ситуаций, когда не удалось корректно определить или инициализировать устройства метрологической системы.
    /// </summary>
    /// <param name="ex">Исключение, вызвавшее ошибку (оригинальная причина).</param>
    /// <returns>Экземпляр <see cref="SystemExceptionBase"/> с описанием ошибки сбора устройств.</returns>
    public static SystemExceptionBase DeviceCollectFailed(Exception ex) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_DeviceCollectFailed,
        Description = $"Ошибка при сборе устройств: {ex.Message}"
      });

    /// <summary>
    /// Исключение: не удалось разобрать одну или обе точки подключения.
    /// </summary>
    public static SystemExceptionBase PointParsingFailed() =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_PointParsingFailed,
        Description = "Не удалось определить одну или обе точки подключения. Проверьте правильность формата (например, 1.2.3)."
      });

    /// <summary>
    /// Исключение: указанный метрологический режим не распознан или не поддерживается.
    /// </summary>
    /// <param name="mode">Имя режима (например, "DCW", "IR", "ACW").</param>
    public static SystemExceptionBase UnknownMetrologicalMode(string mode) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_UnknownMetrologicalMode,
        Description = $"Метрологический режим \"{mode}\" не распознан или не поддерживается системой."
      });

    /// <summary>
    /// Исключение: устройство с указанной метрологической ролью не найдено или имеет неверный тип.
    /// </summary>
    /// <param name="role">Роль устройства (например, KC, IE, PR, CI).</param>
    /// <param name="index">Индекс устройства, если их несколько одной роли.</param>
    /// <param name="expectedType">Ожидаемый тип интерфейса устройства.</param>
    public static SystemExceptionBase DeviceByRoleNotFound(MeasurementTypeCommand role, int index, Type expectedType) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_Validation_DeviceByRoleNotFound,
        Description = $"Устройство с ролью {role} (index: {index}) не найдено или не реализует интерфейс {expectedType.Name}."
      });
  }
}
