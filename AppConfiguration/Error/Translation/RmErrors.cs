using System;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
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
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem CannotParseExpression(string expr, int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = $"Не удалось распознать выражение: {expr}"
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
      LineNumber = startLineNumber,
      Command = command,
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
      LineNumber = startLineNumber,
      Command = command,
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
      LineNumber = startLineNumber,
      Command = command,
      Description = "Нельзя разбить диапазон слева на равные группы под массив справа!"
    };

    /// <summary>
    /// Ошибка: правая группа короче, чем левая.
    /// </summary>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem GroupTooShort(int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = "Правая группа короче, чем левая!"
    };

    /// <summary>
    /// Ошибка: диапазоны со сдвигом не совпадают по количеству элементов.
    /// </summary>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem StepRangeMismatch(int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = "Диапазоны не совпадают по количеству элементов."
    };

    /// <summary>
    /// Ошибка: команда РМ не содержит ни одного параметра.
    /// </summary>
    /// <param name="model">Модель RM-команды.</param>
    /// <returns>Экземпляр ошибки <see cref="ErrorItem"/>.</returns>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = "Команда РМ должна содержать хотя бы один параметр. Тело команды не может быть пустым."
    };
  }
}
