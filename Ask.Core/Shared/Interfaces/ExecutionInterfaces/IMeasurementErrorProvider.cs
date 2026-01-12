using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Определяет контракт для получения метрологических погрешностей
  /// по типу команды и значению измеряемой величины.
  /// </summary>
  public interface IMeasurementErrorProvider
  {
    /// <summary>
    /// Возвращает числовую и процентную погрешности,
    /// соответствующие указанному типу команды и измеренному значению.
    /// </summary>
    /// <param name="type">
    /// Тип команды (режим метрологии), для которого требуется определить погрешности.
    /// </param>
    /// <param name="measuredValue">
    /// Значение измеряемой величины, на основании которого подбирается диапазон.
    /// </param>
    /// <returns>
    /// Кортеж, содержащий:
    /// <list type="bullet">
    ///   <item><description><c>Numeric</c> — абсолютная (числовая) погрешность.</description></item>
    ///   <item><description><c>Percent</c> — относительная (в процентах) погрешность.</description></item>
    /// </list>
    /// </returns>
    (double Numeric, double Percent) GetErrorParameters(MeasurementTypeCommand type, double measuredValue);

    /// <summary>
    /// Возвращает диапазон допустимых значений измеряемой величины
    /// с учётом погрешностей для заданного типа команды.
    /// </summary>
    /// <param name="typeCommand">
    /// Тип команды (режим метрологии), для которого вычисляется диапазон.
    /// </param>
    /// <param name="expectedValue">
    /// Ожидаемое (номинальное) значение измерения.
    /// </param>
    /// <returns>
    /// Кортеж, содержащий:
    /// <list type="bullet">
    ///   <item><description><c>Min</c> — нижняя граница допустимого диапазона.</description></item>
    ///   <item><description><c>Max</c> — верхняя граница допустимого диапазона.</description></item>
    /// </list>
    /// </returns>
    (double Min, double Max) GetRange(MeasurementTypeCommand typeCommand, double expectedValue);

    /// <summary>
    /// Возвращает все метрологические данные (типы команд и их диапазоны погрешностей),
    /// включая полную структуру диапазонов для каждой команды.
    /// </summary>
    /// <returns>
    /// Коллекция сущностей <see cref="MeasurementErrorEntity"/>,
    /// каждая из которых содержит связанные диапазоны (<c>Ranges</c>).
    /// </returns>
    IEnumerable<T> GetAllWithRanges<T>();
  }
}
