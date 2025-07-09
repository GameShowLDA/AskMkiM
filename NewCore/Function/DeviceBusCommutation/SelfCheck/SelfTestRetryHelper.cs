using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using Utilities;
using Utilities.Models;

namespace NewCore.Function.DeviceBusCommutation.SelfCheck
{
  /// <summary>
  /// Предоставляет вспомогательные методы с поддержкой повтора при неудачном выполнении операций самотестирования.
  /// Позволяет регистрировать действия повтора и отображать соответствующие сообщения.
  /// </summary>
  static internal class SelfTestRetryHelper
  {
    /// <summary>
    /// Выполняет попытку замкнуть указанную цепь с возможностью повтора при ошибке.
    /// Если замыкание не удалось, отображается сообщение об ошибке и сохраняется действие для кнопки "Повторить".
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений и управления действиями повтора.</param>
    /// <param name="selfTestChecker">Объект, выполняющий замыкание цепи.</param>
    /// <param name="testType">Тип соединения (например, BlockingRelay, Multimeter и т. д.).</param>
    /// <param name="busContact">Номер контакта шины, подлежащий замыканию.</param>
    /// <param name="circuitName">Название цепи для отображения в сообщениях.</param>
    /// <returns>True, если замыкание выполнено успешно; иначе false.</returns>
    internal static async Task<bool> TryCloseCircuitWithRetryAsync(CancellationToken cancellation, IUserMessageService messageService, ISelfTestCheckerDeviceBusCommutation selfTestChecker, TypeConnector testType, int busContact, string circuitName)
    {
      cancellation.ThrowIfCancellationRequested();

      if (!await selfTestChecker.ExecuteSelfTestAsync(cancellation, testType, busContact, 1))
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Ошибка при подключении: {circuitName}.", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 });
        return false;
      }
      else
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"\"{circuitName}\" подключен", type: ShowMessageModel.MessageType.Success) { IndentLevel = 1 });
        return true;
      }
    }

    /// <summary>
    /// Выполняет проверку состояния реле через <see cref="ContinuityManager"/> и отображает результат.
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <param name="meter">Измерительное устройство, содержащее ContinuityManager.</param>
    /// <param name="relay">Название реле для отображения в сообщении.</param>
    /// <returns>True, если проверка показала отсутствие цепи (нормально разомкнутое реле); иначе false.</returns>
    internal static async Task<bool> CheckRelayStateAsync(
        CancellationToken cancellation,
        IUserMessageService messageService,
        IFastMeter meter,
        int relay)
    {
      cancellation.ThrowIfCancellationRequested();

      var result = await meter.ContinuityManager.CheckContinuityAsync(false);
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
  }
}
