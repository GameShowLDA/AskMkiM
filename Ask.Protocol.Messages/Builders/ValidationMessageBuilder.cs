using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует сообщения об ошибках входных данных, параметров, конфигурации и доступности зависимостей.
/// </summary>
internal static class ValidationMessageBuilder
{
  /// <summary>
  /// Формирует сообщение о некорректном номере проверяемого блока.
  /// </summary>
  /// <returns>Сообщение об ошибке номера проверяемого блока.</returns>
  internal static ShowMessageModel BuildInvalidTestedNumber()
  {
    return BuildFieldError("Поле 'Номер проверяемого' заполнено некорректно!");
  }

  /// <summary>
  /// Формирует сообщение о некорректном номере проверяющего блока.
  /// </summary>
  /// <returns>Сообщение об ошибке номера проверяющего блока.</returns>
  internal static ShowMessageModel BuildInvalidTesterNumber()
  {
    return BuildFieldError("Поле 'Номер проверяющего' заполнено некорректно!");
  }

  /// <summary>
  /// Формирует сообщение о совпадении номеров проверяемого и проверяющего блоков.
  /// </summary>
  /// <returns>Сообщение о совпадении номеров блоков.</returns>
  internal static ShowMessageModel BuildDuplicateNumbers()
  {
    return BuildFieldError("Номера проверяемого и проверяющего блоков совпадают!");
  }

  /// <summary>
  /// Формирует сообщение об отсутствующем диапазоне проверки.
  /// </summary>
  /// <returns>Сообщение о незаполненном диапазоне проверки.</returns>
  internal static ShowMessageModel BuildEmptyRange()
  {
    return BuildFieldError("Поле 'Диапазон проверки' не заполнено!");
  }

  /// <summary>
  /// Формирует сообщение о некорректном диапазоне проверки.
  /// </summary>
  /// <param name="details">Описание обнаруженной ошибки диапазона.</param>
  /// <returns>Сообщение об ошибке диапазона проверки.</returns>
  internal static ShowMessageModel BuildInvalidRange(string details)
  {
    return BuildFieldError($"Неверный диапазон: {details}");
  }

  /// <summary>
  /// Формирует сообщение об отсутствии измерителя для выполнения самоконтроля.
  /// </summary>
  /// <returns>Сообщение об отсутствии измерителя.</returns>
  internal static ShowMessageModel BuildMeterUnavailable()
  {
    return BuildSelectionError("Не удалось получить измеритель.");
  }

  /// <summary>
  /// Формирует сообщение об отсутствии выбранного устройства для выполнения самоконтроля.
  /// </summary>
  /// <returns>Сообщение об отсутствии выбранного устройства.</returns>
  internal static ShowMessageModel BuildDeviceUnavailable()
  {
    return BuildSelectionError("Не удалось получить устройство.");
  }

  private static ShowMessageModel BuildFieldError(string header)
  {
    return new ShowMessageModel(header, ShowMessageModel.ErrorMessage.TitleColor);
  }

  private static ShowMessageModel BuildSelectionError(string details)
  {
    return new ShowMessageModel(
      "Ошибка",
      message: details,
      type: ShowMessageModel.MessageType.Error);
  }
}
