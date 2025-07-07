using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Additionally;
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
    internal static async Task<bool> TryCloseCircuitWithRetryAsync(IUserMessageService messageService, ISelfTestCheckerDeviceBusCommutation selfTestChecker, TypeConnector testType, int busContact, string circuitName)
    {
      async Task retryAction()
      {
        await TryCloseCircuitWithRetryAsync(messageService, selfTestChecker, testType, busContact, circuitName);
      }

      if (!await selfTestChecker.ExecuteSelfTestAsync(testType, busContact, 1))
      {
        messageService.RegisterRetryAction(retryAction);
        await messageService.ShowMessageAsync(new ShowMessageModel($"Ошибка при подключении: {circuitName}.", type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 })
        ;
        return false;
      }
      else
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Цепь \"{circuitName}\" подключена", type: ShowMessageModel.MessageType.Success) { IndentLevel = 1 });
        return true;
      }
    }
  }
}
