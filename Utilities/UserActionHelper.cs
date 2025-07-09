using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Utilities.IUserMessageService;

namespace Utilities
{
  public static class UserActionHelper
  {
    /// <summary>
    /// Универсальный цикл для выполнения операции с поддержкой повтора, пропуска и завершения.
    /// </summary>
    /// <param name="operation">Функция, возвращающая true при успехе и false при ошибке.</param>
    /// <param name="messageService">Сервис сообщений для ожидания действия пользователя.</param>
    public static async Task RunWithUserRepeatAsync(
        Func<Task<bool>> operation,
        IUserMessageService messageService)
    {
      bool error = false;
      bool next = true;

      do 
      {
        bool success = await operation();

        if (success && next)
        {
          return;
        }
        else if (!error)
        {
          error = true;
          next = false;
        }

        var action = await messageService.WaitUserActionAsync();
        if (action == UserAction.Retry)
        {
          continue;
        }
        else
        {
          next = true;
          error = false;
          return;
        }
      }
      while (error);
    }
  }
}
