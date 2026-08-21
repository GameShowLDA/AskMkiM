using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.Builders;
using Ask.Protocol.Messages.Show;
using System.Runtime.CompilerServices;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единую точку формирования, логирования и вывода допустимых диапазонов.
/// </summary>
public static class RangeMessages
{
  /// <summary>
  /// Публикует допустимый диапазон значений.
  /// </summary>
  /// <param name="measurementUnit">Единица измерения.</param>
  /// <param name="measurementRange">Границы допустимого диапазона.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <param name="indentLevel">Уровень отступа сообщения.</param>
  /// <param name="header">Заголовок сообщения.</param>
  /// <param name="callerName">Имя метода, вызвавшего публикацию.</param>
  /// <param name="callerFile">Путь к файлу, вызвавшему публикацию.</param>
  /// <param name="callerLine">Номер строки, вызвавшей публикацию.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="measurementUnit"/>,
  /// <paramref name="measurementRange"/> или <paramref name="outputService"/>
  /// равен <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Выбрасывается, если <paramref name="header"/> не содержит значимых символов.
  /// </exception>
  public static Task PublishAllowedRangeAsync(
    Enum measurementUnit,
    MeasurementRange measurementRange,
    IMessageOutputService outputService,
    int indentLevel = 0,
    string header = "Диапазон допускаемых значений",
    [CallerMemberName] string callerName = "",
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0)
  {
    ArgumentNullException.ThrowIfNull(measurementUnit);
    ArgumentNullException.ThrowIfNull(measurementRange);
    ArgumentNullException.ThrowIfNull(outputService);
    ArgumentException.ThrowIfNullOrWhiteSpace(header);

    ShowMessageModel message = RangeMessageBuilder.BuildAllowedRange(
      measurementUnit,
      measurementRange,
      header);
    message.IndentLevel = indentLevel;

    return RangeMessagePublisher.PublishAsync(
      message,
      outputService,
      callerName,
      callerFile,
      callerLine);
  }
}
