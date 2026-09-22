using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Core.Services.Errors.Metrology
{
  /// <summary>
  /// Содержит ошибки расчёта допустимого диапазона метрологических измерений.
  /// </summary>
  public static class MeasurementErrorCalculationErrors
  {
    /// <summary>
    /// Создаёт исключение для команды без эталонной конфигурации погрешностей.
    /// </summary>
    /// <param name="type">Тип метрологической команды.</param>
    /// <returns>Исключение с описанием отсутствующей конфигурации.</returns>
    public static SystemExceptionBase DefaultsNotFound(MeasurementTypeCommand type) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_MeasurementError_DefaultsNotFound,
        Description = $"Не найдены эталонные погрешности для команды: {type}"
      });

    /// <summary>
    /// Создаёт исключение, если для значения не найден подходящий диапазон погрешности.
    /// </summary>
    /// <param name="type">Тип метрологической команды.</param>
    /// <returns>Исключение с описанием отсутствующего диапазона.</returns>
    public static SystemExceptionBase ToleranceRangeNotFound(MeasurementTypeCommand type) =>
      new(new ErrorItem
      {
        Code = ErrorCode.Metrology_MeasurementError_ToleranceRangeNotFound,
        Description = $"Не удалось определить диапазон погрешности для команды {type}"
      });
  }
}
