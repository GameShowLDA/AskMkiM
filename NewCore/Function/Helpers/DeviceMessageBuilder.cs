using AppConfiguration.Services;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
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
    public static ShowMessageModel GetDefaultSettings(IAttachableDevice device)
    {
      var model = new ShowMessageModel(header: $"{device.Name}({device.NumberChassis}.{device.Number})")
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
    public static ShowMessageModel BuildMessage(ref ShowMessageModel message, string baseMessage = null, bool isError = false)
    {
      if (baseMessage == null)
      {
        baseMessage = string.Empty;
      }

      if (isError)
      {
        message.Message = $"{baseMessage} [{ShowMessageModel.ErrorMessage.Item1}]";
        message.MessageColor = ShowMessageModel.ErrorMessage.Item2;
      }
      else
      {
        message.Message += $"{baseMessage} [{ShowMessageModel.SuccessMessage.Item1}]";
        message.MessageColor = ShowMessageModel.SuccessMessage.Item2;
      }

      return message;
    }

    public static async Task ShowDeviceMessage(ShowMessageModel showMessageModel, bool result)
    {
      if (!result || await AppConfiguration.Protocol.ProtocolConfig.GetDeviceInfo())
      {
        await UserMessageServiceProvider.Instance.ShowMessageAsync(showMessageModel);
      }
    }

    /// <summary>
    /// Унифицированный метод отображения сообщения о подключении или отключении устройства.
    /// </summary>
    /// <param name="device">Устройство, для которого формируется сообщение.</param>
    /// <param name="headerSuffix">Текст, добавляемый к заголовку.</param>
    /// <param name="result">Результат операции (true — успех, false — ошибка).</param>
    /// <returns>Задача выполнения показа сообщения.</returns>
    public static async Task ShowConnectionMessageAsync(IAttachableDevice device, string headerSuffix, bool result, int indentLevel)
    {
      var showMessageModel = GetDefaultSettings(device);
      showMessageModel.Header += $" - {headerSuffix}";
      showMessageModel.IndentLevel = indentLevel;
      BuildMessage(ref showMessageModel, isError: !result);
      await ShowDeviceMessage(showMessageModel, result);
    }

    /// <summary>
    /// Унифицированный метод отображения сообщения о подключении или отключении устройства с дополнительным текстом.
    /// </summary>
    /// <param name="device">Устройство, для которого формируется сообщение.</param>
    /// <param name="headerSuffix">Текст, добавляемый к заголовку.</param>
    /// <param name="baseMessage">Основной текст сообщения (например, номер).</param>
    /// <param name="result">Результат операции (true — успех, false — ошибка).</param>
    /// <returns>Задача выполнения показа сообщения.</returns>
    public static async Task ShowConnectionMessageAsync(IAttachableDevice device, string headerSuffix, string baseMessage, bool result, int indentLevel)
    {
      var showMessageModel = GetDefaultSettings(device);
      showMessageModel.Header += $" - {headerSuffix}";
      showMessageModel.IndentLevel = indentLevel;
      BuildMessage(ref showMessageModel, baseMessage, isError: !result);
      await ShowDeviceMessage(showMessageModel, result);
    }

  }
}
