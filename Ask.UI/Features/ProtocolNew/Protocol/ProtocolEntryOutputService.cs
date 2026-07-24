using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;

namespace Ask.UI.Features.ProtocolNew.Protocol
{
  /// <summary>
  /// Подготавливает и выводит одну запись протокола в существующий редактор.
  /// Сервис не управляет паузой, пошаговым режимом и жизненным циклом исполнителя.
  /// </summary>
  internal sealed class ProtocolEntryOutputService
  {
    /// <summary>Приёмник подготовленных записей протокола.</summary>
    private readonly IProtocolEntrySink _sink;

    /// <summary>Последняя запись, участвующая в сокращении детального протокола.</summary>
    private ShowMessageModel? _lastMessage;

    /// <summary>
    /// Создаёт сервис вывода одной записи.
    /// </summary>
    /// <param name="sink">Приёмник записей редактора.</param>
    public ProtocolEntryOutputService(IProtocolEntrySink sink)
    {
      _sink = sink;
    }

    /// <summary>
    /// Подготавливает модель, при необходимости накапливает ошибку и добавляет запись в редактор.
    /// </summary>
    /// <param name="message">Исходная модель записи.</param>
    /// <param name="isLastMessage">Признак последней записи текущего блока.</param>
    /// <param name="ignoreOutputValidation">Разрешает вывод служебной записи без текста сообщения.</param>
    /// <param name="accumulateErrors">Определяет, требуется ли накопление сообщений об ошибках.</param>
    /// <param name="checkType">Тип текущей проверки.</param>
    /// <param name="addError">Операция регистрации накопленной ошибки.</param>
    /// <param name="callerName">Имя метода, сформировавшего запись.</param>
    /// <param name="callerFile">Путь к файлу, сформировавшему запись.</param>
    /// <param name="callerLine">Номер строки, сформировавшей запись.</param>
    /// <returns><c>true</c>, если запись была добавлена в редактор.</returns>
    public async Task<bool> WriteAsync(
      ShowMessageModel message,
      bool isLastMessage,
      bool ignoreOutputValidation,
      bool accumulateErrors,
      CheckType checkType,
      Action<string> addError,
      string callerName,
      string callerFile,
      int callerLine)
    {
      AddExecutionTime(message);
      AddDebugSource(message, callerName, callerFile, callerLine);
      await ApplyDetailedProtocolModeAsync(message);
      AccumulateError(message, accumulateErrors, checkType, addError);
      ApplyStatusAndHighlighting(message);

      if (!CanDisplay(message, ignoreOutputValidation))
      {
        return false;
      }

      await _sink.AppendLineAsync(message, isLastMessage);
      return true;
    }

    /// <summary>Сбрасывает состояние последней записи после очистки протокола.</summary>
    public void Reset()
    {
      _lastMessage = null;
    }

    /// <summary>Добавляет время выполнения, если оно включено настройками протокола.</summary>
    private static void AddExecutionTime(ShowMessageModel message)
    {
      if (ProtocolConfig.GetTimeStart() && message.Status != MessageType.Info && message.Status != MessageType.Command)
      {
        message.Time = SystemStateManager._stopwatch.Elapsed.ToString(@"mm\:ss\.fff", CultureInfo.InvariantCulture);
      }
    }

    /// <summary>Добавляет сведения о месте формирования записи при наличии отладочных прав.</summary>
    private static void AddDebugSource(
      ShowMessageModel message,
      string callerName,
      string callerFile,
      int callerLine)
    {
      if (!AdminConfig.GetDebugRights())
      {
        return;
      }

      var source = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})";
      message.Debug = string.IsNullOrEmpty(message.Debug)
        ? source
        : $"{message.Debug}|| {source}";
    }

    /// <summary>Удаляет заменяемую запись при отключённом детальном протоколе.</summary>
    private async Task ApplyDetailedProtocolModeAsync(ShowMessageModel message)
    {
      if (ProtocolConfig.GetShowDetailedProtocol())
      {
        return;
      }

      if (_lastMessage != null && _lastMessage.CanBeDeleted && !_lastMessage.ExecutionError)
      {
        await _sink.RemoveLastLinesAsync();
      }

      _lastMessage = message;
    }

    /// <summary>Передаёт текст ошибочной записи существующему накопителю ошибок.</summary>
    private static void AccumulateError(
      ShowMessageModel message,
      bool accumulateErrors,
      CheckType checkType,
      Action<string> addError)
    {
      if (!accumulateErrors
        || message.Status != MessageType.Error
        || ShouldSkipAccumulatedError(message, checkType))
      {
        return;
      }

      var error = message.ExecutionErrorMessage ?? message.ToString();
      if (!string.IsNullOrWhiteSpace(error))
      {
        addError(error);
      }
    }

    /// <summary>
    /// Исключает из итогового заключения самоконтроля дублирующие внутренние результаты мультиметра.
    /// </summary>
    internal static bool ShouldSkipAccumulatedError(ShowMessageModel message, CheckType checkType)
    {
      if (checkType != CheckType.SelfTest)
      {
        return false;
      }

      return message.Header.StartsWith("Результат \"Измерение ", StringComparison.Ordinal)
        || message.Header.Contains(" - Измерение ", StringComparison.Ordinal);
    }

    /// <summary>Добавляет обозначение качества и применяет цвета записи.</summary>
    private static void ApplyStatusAndHighlighting(ShowMessageModel message)
    {
      if (message.Status != MessageType.Info)
      {
        var prefix = message.GetQualityPrefix();
        if (string.IsNullOrEmpty(message.Message))
        {
          message.Message += prefix;
        }
        else if (!message.Message.Contains(prefix))
        {
          message.Message += " " + prefix;
        }

        message.MessageColor = message.GetColorMessage();
      }

      if (!UserInterfaceConfig.GetSyntaxHighlighting())
      {
        var color = (Color)Application.Current.Resources["tests.protocol.message.header.foreground"];
        message.HeaderColor = color;
        message.MessageColor = color;
        message.TimeColor = color;
        message.HeaderBackgroundColor = null;
      }
    }

    /// <summary>Проверяет, содержит ли модель данные, разрешённые для вывода.</summary>
    private static bool CanDisplay(ShowMessageModel message, bool ignoreOutputValidation)
    {
      if (ignoreOutputValidation)
      {
        return true;
      }

      return !string.IsNullOrEmpty(message.Message)
        || message.Status == MessageType.Command
        || ProtocolConfig.GetHeaderInfo();
    }
  }
}
