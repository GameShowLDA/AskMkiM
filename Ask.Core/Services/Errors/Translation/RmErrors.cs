using Ask.Core.Services.Errors.Models;

namespace Ask.Core.Services.Errors.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений RM-команд.
  /// </summary>
  public static class RmErrors
  {
    /// <summary>
    /// Ошибка: выражение не распознано.
    /// </summary>
    /// <param name="expr">Исходное выражение.</param>
    /// <param name="startLineNumber">Индекс символа строки,где начинается ошибка.</param>
    /// <param name="command">Команда.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem CannotParseExpression(string expr, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_CannotParseExpression,
      Description = $"Не удалось распознать выражение: {expr}"
    };

    /// <summary>
    /// Ошибка: возможно присутствует лишний пробел.
    /// </summary>
    /// <param name="expr">Исходное выражение.</param>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem ExtraSpace(string expr, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_ExtraSpace,
      Description = $"Не удалось распознать выражение: {expr}. Возможно присутствует лишний пробел."
    };

    /// <summary>
    /// Ошибка: возможно присутствует лишний пробел.
    /// </summary>
    /// <param name="expr">Исходное выражение.</param>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem UnacceptableSymbol(string expr, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_UnacceptableSymbol,
      Description = $"Обнаружены недопустимые символы в выражении: {expr}."
    };

    /// <summary>
    /// Ошибка: левая или правая часть выражения пустая.
    /// </summary>
    /// <param name="left">Левая часть выражения.</param>
    /// <param name="middle">Синоним (если есть).</param>
    /// <param name="right">Правая часть выражения.</param>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem EmptyLeftOrRight(string left, string middle, string right, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_EmptyLeftOrRight,
      Description = $"Левая или правая часть выражения пуста: {left} == {middle} = {right}"
    };

    /// <summary>
    /// Ошибка: количество точек слева и справа не совпадает.
    /// </summary>
    /// <param name="leftCount">Количество элементов слева.</param>
    /// <param name="rightCount">Количество элементов справа.</param>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem MismatchedCounts(int leftCount, int rightCount, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_MismatchedCounts,
      Description = $"Количество точек ОК и входов должно совпадать! " +
                    $"Левая часть: (количество: {leftCount}) " +
                    $"Правая часть: (количество: {rightCount})"
    };

    /// <summary>
    /// Ошибка: невозможно разбить диапазон слева на равные группы.
    /// </summary>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem GroupMismatch(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_GroupMismatch,
      Description = "Нельзя разбить диапазон слева на равные группы под массив справа!"
    };

    /// <summary>
    /// Ошибка: правая группа короче, чем левая.
    /// </summary>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem GroupTooShort(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_GroupTooShort,
      Description = "Правая группа короче, чем левая!"
    };

    /// <summary>
    /// Ошибка: диапазоны со сдвигом не совпадают по количеству элементов.
    /// </summary>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem StepRangeMismatch(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_StepRangeMismatch,
      Description = "Диапазоны не совпадают по количеству элементов."
    };

    /// <summary>
    /// Ошибка: команда РМ не содержит ни одного параметра.
    /// </summary>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Rm_EmptyCommandBody,
      Description = "Команда РМ должна содержать хотя бы один параметр. Тело команды не может быть пустым."
    };
  }
}
