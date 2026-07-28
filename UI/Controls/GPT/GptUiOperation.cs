using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.LogLib;

namespace UI.Controls.GPT
{
  /// <summary>
  /// Выполняет операции административного интерфейса GPT без распространения аппаратных ошибок в UI-поток.
  /// </summary>
  internal static class GptUiOperation
  {
    /// <summary>
    /// Возвращает выбранную пробойную установку.
    /// </summary>
    /// <returns>Пробойная установка, загруженная административным интерфейсом.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если пробойная установка не настроена или не загружена.
    /// </exception>
    internal static IBreakdownTester GetDevice()
    {
      return GPTPunchControl.ModelGPT
        ?? throw new InvalidOperationException(
          "Пробойная установка GPT не найдена. Проверьте конфигурацию оборудования.");
    }

    /// <summary>
    /// Записывает ошибку операции GPT в общий журнал приложения.
    /// </summary>
    /// <param name="operation">Название операции.</param>
    /// <param name="exception">Возникшее исключение.</param>
    internal static void ReportError(string operation, Exception exception)
    {
      LoggerUtility.LogError(
        $"GPT — {operation}: {exception.Message}",
        isDeviceLog: true);
    }

    /// <summary>
    /// Проверяет результат аппаратной команды GPT.
    /// </summary>
    /// <param name="result">Результат команды.</param>
    /// <param name="operation">Название операции.</param>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если устройство отклонило команду.
    /// </exception>
    internal static void EnsureSuccess(
      (bool Success, string Message) result,
      string operation)
    {
      if (!result.Success)
      {
        throw new InvalidOperationException(
          string.IsNullOrWhiteSpace(result.Message)
            ? $"Устройство не выполнило операцию «{operation}»."
            : result.Message);
      }
    }
  }
}
