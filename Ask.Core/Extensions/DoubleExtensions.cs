namespace Ask.Core.Extensions;

/// <summary>
/// Предоставляет методы расширения для работы со значениями типа <see cref="double"/>.
/// </summary>
public static class DoubleExtensions
{
  /// <summary>
  /// Проверяет, равно ли значение нулю.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение равно нулю;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsZero(this double value) =>
    value == 0;

  /// <summary>
  /// Проверяет, не равно ли значение нулю.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение не равно нулю;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsNotZero(this double value) =>
    value != 0;

  /// <summary>
  /// Проверяет, является ли значение положительным.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение больше нуля;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsPositive(this double value) =>
    value > 0;

  /// <summary>
  /// Проверяет, является ли значение отрицательным.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение меньше нуля;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsNegative(this double value) =>
    value < 0;

  /// <summary>
  /// Проверяет, является ли значение целым числом.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение не содержит дробной части;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsInteger(this double value) =>
    !double.IsNaN(value) &&
    !double.IsInfinity(value) &&
    value % 1 == 0;

  /// <summary>
  /// Проверяет, является ли значение конечным числом.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение не является <c>NaN</c> или бесконечностью;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsFinite(this double value) =>
    !double.IsNaN(value) &&
    !double.IsInfinity(value);

  /// <summary>
  /// Проверяет, является ли значение нечисловым значением <c>NaN</c>.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение равно <c>NaN</c>;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsNaN(this double value) =>
    double.IsNaN(value);

  /// <summary>
  /// Проверяет, является ли значение положительной или отрицательной бесконечностью.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение является бесконечностью;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsInfinity(this double value) =>
    double.IsInfinity(value);

  /// <summary>
  /// Возвращает целую часть числа без округления.
  /// </summary>
  /// <param name="value">Исходное значение.</param>
  /// <returns>Целая часть числа.</returns>
  /// <remarks>
  /// Для отрицательных значений дробная часть отбрасывается в сторону нуля.
  /// Например, <c>-12.75</c> преобразуется в <c>-12</c>.
  /// </remarks>
  public static double IntegerPart(this double value) =>
    Math.Truncate(value);

  /// <summary>
  /// Возвращает дробную часть числа.
  /// </summary>
  /// <param name="value">Исходное значение.</param>
  /// <returns>Дробная часть числа.</returns>
  /// <remarks>
  /// Знак дробной части соответствует знаку исходного значения.
  /// Например, для <c>12.75</c> результатом будет <c>0.75</c>,
  /// а для <c>-12.75</c> — <c>-0.75</c>.
  /// </remarks>
  public static double FractionalPart(this double value) =>
    value - Math.Truncate(value);

  /// <summary>
  /// Возвращает модуль числа.
  /// </summary>
  /// <param name="value">Исходное значение.</param>
  /// <returns>Абсолютное значение числа.</returns>
  public static double Abs(this double value) =>
    Math.Abs(value);

  /// <summary>
  /// Проверяет, находится ли значение в указанном диапазоне.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <param name="minimum">Минимальная граница диапазона.</param>
  /// <param name="maximum">Максимальная граница диапазона.</param>
  /// <returns>
  /// <see langword="true"/>, если значение находится между границами включительно;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsBetween(
    this double value,
    double minimum,
    double maximum) =>
    value >= minimum && value <= maximum;

  /// <summary>
  /// Ограничивает значение указанным диапазоном.
  /// </summary>
  /// <param name="value">Исходное значение.</param>
  /// <param name="minimum">Минимально допустимое значение.</param>
  /// <param name="maximum">Максимально допустимое значение.</param>
  /// <returns>
  /// Исходное значение, если оно находится в диапазоне;
  /// минимальную или максимальную границу, если значение выходит за диапазон.
  /// </returns>
  public static double Clamp(
    this double value,
    double minimum,
    double maximum) =>
    Math.Clamp(value, minimum, maximum);

  /// <summary>
  /// Проверяет, равны ли два значения с учётом допустимой погрешности.
  /// </summary>
  /// <param name="value">Первое значение.</param>
  /// <param name="expected">Ожидаемое значение.</param>
  /// <param name="tolerance">Допустимая абсолютная погрешность.</param>
  /// <returns>
  /// <see langword="true"/>, если абсолютная разница между значениями
  /// не превышает допустимую погрешность;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Возникает, если <paramref name="tolerance"/> меньше нуля.
  /// </exception>
  public static bool IsApproximately(
    this double value,
    double expected,
    double tolerance)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(tolerance);

    return Math.Abs(value - expected) <= tolerance;
  }

  /// <summary>
  /// Проверяет, является ли значение близким к нулю
  /// с учётом допустимой погрешности.
  /// </summary>
  /// <param name="value">Проверяемое значение.</param>
  /// <param name="tolerance">Допустимая абсолютная погрешность.</param>
  /// <returns>
  /// <see langword="true"/>, если абсолютное значение числа
  /// не превышает допустимую погрешность;
  /// в противном случае — <see langword="false"/>.
  /// </returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Возникает, если <paramref name="tolerance"/> меньше нуля.
  /// </exception>
  public static bool IsApproximatelyZero(
    this double value,
    double tolerance)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(tolerance);

    return Math.Abs(value) <= tolerance;
  }

  /// <summary>
  /// Округляет значение до указанного количества знаков после запятой.
  /// </summary>
  /// <param name="value">Исходное значение.</param>
  /// <param name="digits">Количество знаков после запятой.</param>
  /// <returns>Округлённое значение.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Возникает, если количество знаков выходит за допустимый диапазон.
  /// </exception>
  public static double Round(
    this double value,
    int digits) =>
    Math.Round(value, digits);

  /// <summary>
  /// Округляет значение вниз до ближайшего целого числа.
  /// </summary>
  /// <param name="value">Исходное значение.</param>
  /// <returns>Наибольшее целое число, не превышающее исходное значение.</returns>
  public static double Floor(this double value) =>
    Math.Floor(value);

  /// <summary>
  /// Округляет значение вверх до ближайшего целого числа.
  /// </summary>
  /// <param name="value">Исходное значение.</param>
  /// <returns>Наименьшее целое число, не меньшее исходного значения.</returns>
  public static double Ceiling(this double value) =>
    Math.Ceiling(value);
}