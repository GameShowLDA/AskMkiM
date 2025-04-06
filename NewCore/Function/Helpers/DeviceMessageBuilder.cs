using NewCore.Base.Device;
using NewCore.Device;
using Utilities.Models;

namespace NewCore.Function.Helpers
{
  /// <summary>
  /// Класс для формирования сообщений, связанных с устройством.
  /// </summary>
  public static class DeviceMessageBuilder
  {
    /// <summary>
    /// Возвращает заголовок сообщения в формате "Имя[Номер]".
    /// </summary>
    /// <param name="device">Коммутационное устройство.</param>
    /// <returns>Форматированный заголовок.</returns>
    public static ShowMessageModel GetDefaultSettings(IDevice device)
    {
      var model = new ShowMessageModel(header: $"{device.Name}[{device.Number}]")
      {
        IsDeviceMessage = true
      };
      
      return model;
    }

    /// <summary>
    /// Формирует стандартное сообщение на основе результата.
    /// </summary>
    /// <param name="baseMessage">Основной текст сообщения.</param>
    /// <param name="message">Исходная модель сообщения.</param>
    /// <param name="isError">Признак ошибки.</param>
    /// <returns>Сформированная модель сообщения.</returns>
    public static ShowMessageModel BuildMessage(string baseMessage, ShowMessageModel message, bool isError)
    {
      if (isError)
      {
        message.Message += $" [{ShowMessageModel.ErrorMessage.Item1}]";
        message.MessageColor = ShowMessageModel.ErrorMessage.Item2;
      }
      else
      {
        message.Message += $" [{ShowMessageModel.SuccessMessage.Item1}]";
      }

      return message;
    }
  }
}
