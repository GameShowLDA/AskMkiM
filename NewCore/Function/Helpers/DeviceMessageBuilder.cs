using AppConfiguration.Services;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Device;
using Utilities.Models;
using static Utilities.LoggerUtility;

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
        IsDeviceMessage = true,
      };

      return model;
    }

    /// <summary>
    /// Формирует сообщение для отображения пользователю с указанием успешности или ошибки.
    /// </summary>
    /// <param name="message">Модель сообщения, в которую будет записан текст и цвет.</param>
    /// <param name="baseMessage">Базовое текстовое сообщение, добавляемое в начало.</param>
    /// <param name="isError">Флаг, указывающий, является ли сообщение ошибкой.</param>
    /// <returns>Обновлённая модель сообщения <see cref="ShowMessageModel"/>.</returns>
    public static ShowMessageModel BuildMessage(ref ShowMessageModel message, string baseMessage = null, bool isError = false)
    {
      if (baseMessage == null)
      {
        baseMessage = string.Empty;
      }

      return message;
    }

    /// <summary>
    /// Показывает сообщение пользователю, если результат операции отрицательный или устройство не отвечает.
    /// </summary>
    /// <param name="showMessageModel">Модель сообщения для отображения.</param>
    /// <param name="result">Результат выполнения операции: <c>true</c> — успех, <c>false</c> — ошибка.</param>
    public static async Task ShowDeviceMessage(ShowMessageModel showMessageModel, bool result)
    {
      if (!result || await AppConfiguration.Protocol.ProtocolConfig.GetDeviceInfo())
      {
        if (UserMessageServiceProvider.Instance != null)
        {
          await UserMessageServiceProvider.Instance.ShowMessageAsync(showMessageModel, skipPause: true);
        }
        else
        {
          LogError($"{showMessageModel.Header}: {showMessageModel.Message}", isDeviceLog: true);
        }
      }
    }

    /// <summary>
    /// Унифицированный метод отображения сообщения о подключении или отключении устройства.
    /// Формирует заголовок, устанавливает отступ, цвет и текст сообщения в зависимости от результата операции,
    /// а затем отображает сообщение пользователю.
    /// </summary>
    /// <param name="device">Устройство, для которого формируется сообщение.</param>
    /// <param name="headerSuffix">Текст, добавляемый в конец заголовка (например, "Подключено" или "Отключено").</param>
    /// <param name="result">Результат операции: <c>true</c> — успешно, <c>false</c> — ошибка.</param>
    /// <param name="indentLevel">Уровень отступа, используемый при отображении сообщения (визуальная иерархия).</param>
    /// <returns>Задача асинхронного показа сообщения.</returns>
    public static async Task ShowConnectionMessageAsync(IAttachableDevice device, string headerSuffix, bool result, int indentLevel)
    {
      var showMessageModel = GetDefaultSettings(device);
      showMessageModel.Header += $" - {headerSuffix}";
      showMessageModel.IndentLevel = indentLevel;

      if (result)
      {
        showMessageModel.Status = ShowMessageModel.MessageType.Success;
      }
      else
      {
        showMessageModel.Status = ShowMessageModel.MessageType.Error;
      }

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
    /// <param name="indentLevel">Уровень отступа, используемый при отображении сообщения (визуальная иерархия).</param>
    /// <returns>Задача выполнения показа сообщения.</returns>
    public static async Task ShowConnectionMessageAsync(IAttachableDevice device, string headerSuffix, string baseMessage, bool result, int indentLevel)
    {
      var showMessageModel = GetDefaultSettings(device);
      showMessageModel.Header += $" - {headerSuffix}";
      showMessageModel.IndentLevel = indentLevel;
      showMessageModel.Message = baseMessage;

      if (result)
      {
        showMessageModel.Status = ShowMessageModel.MessageType.Success;
      }
      else
      {
        showMessageModel.Status = ShowMessageModel.MessageType.Error;
      }

      BuildMessage(ref showMessageModel, baseMessage, isError: !result);
      await ShowDeviceMessage(showMessageModel, result);
    }
  }
}
