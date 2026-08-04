using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation.SelfCheck
{
  /// <summary>
  /// Предоставляет вспомогательные методы с поддержкой повтора при неудачном выполнении операций самотестирования.
  /// Позволяет регистрировать действия повтора и отображать соответствующие сообщения.
  /// </summary>
  static internal class SelfTestRetryHelper
  {
    /// <summary>
    /// Выполняет проверку состояния реле через <see cref="ContinuityManager"/> и отображает результат.
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <param name="meter">Измерительное устройство, содержащее ContinuityManager.</param>
    /// <param name="relay">Название реле для отображения в сообщении.</param>
    /// <returns>True, если проверка показала отсутствие цепи (нормально разомкнутое реле); иначе false.</returns>
    internal static async Task<bool> CheckRelayStateAsync(
        CancellationToken cancellation,
        IUserInteractionService messageService,
        IMultimeter meter,
        int relay)
    {
      cancellation.ThrowIfCancellationRequested();

      var result = await meter.ContinuityManager.CheckContinuityAsync(false, messageService);
      if (result)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Реле {relay}", type: ShowMessageModel.MessageType.Success) { IndentLevel = 3 });
        return true;
      }
      else
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Реле {relay}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
        return false;
      }
    }

    /// <summary>
    /// Выполняет аппаратную операцию и отображает её результат до ожидания решения оператора.
    /// </summary>
    /// <param name="operation">Аппаратная операция.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="operationMessage">Название аппаратной операции.</param>
    /// <param name="indentLevel">Уровень отступа сообщения.</param>
    /// <returns>
    /// <see langword="true"/>, если операция завершилась успешно.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    internal static async Task<bool> ExecuteHardwareOperationAsync(
      Func<Task<bool>> operation,
      IUserInteractionService messageService,
      string operationMessage,
      int indentLevel = 1)
    {
      bool result = await operation();
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        await messageService.ShowMessageAsync(
          new ShowMessageModel(operationMessage, type: result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)
          {
            IndentLevel = indentLevel,
          },
          skipPause: true);
      }

      return result;
    }
  }
}
