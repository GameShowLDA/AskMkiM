using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Interface;
using static System.Net.Mime.MediaTypeNames;
using static Utilities.Interface.IUserMessageService;

namespace Utilities
{
  public static class UserActionHelper
  {
    /// <summary>
    /// Универсальный цикл для выполнения операции с поддержкой повтора, пропуска и завершения.
    /// </summary>
    /// <param name="operation">Функция, возвращающая true при успехе и false при ошибке.</param>
    /// <param name="messageService">Сервис сообщений для ожидания действия пользователя.</param>
    /// <param name="loop">Всегда вызывает повтор, даже если первое измерение без ошибок.</param>
    public static async Task RunWithUserRepeatAsync(
        Func<Task<bool>> operation,
        IUserMessageService messageService,
        bool loop = false)
    {
      bool error = loop;
      bool next = !loop;

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

        if (messageService.ButtonService != null)
        {
          messageService.ButtonService.ShowOnlyExitButton();
        }

        if (action == UserAction.None)
        {
          return;
        }
        else if (action == UserAction.Retry)
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

    /// <summary>
    /// Универсальный цикл для выполнения операции с поддержкой повтора, пропуска и завершения.
    /// </summary>
    /// <param name="operation">Функция, возвращающая true при успехе и false при ошибке.</param>
    /// <param name="messageService">Сервис сообщений для ожидания действия пользователя.</param>
    /// <param name="loop">Всегда вызывает повтор, даже если первое измерение без ошибок.</param>
    public static async Task<bool> GetRunWithUserRepeatAsync(
        Func<Task<bool>> operation,
        IUserMessageService messageService,
        bool loop = false)
    {
      bool error = loop;
      bool next = !loop;
      bool result = true;

      do
      {
        bool success = await operation();
        result = success;

        if (success && next)
        {
          return result;
        }
        else if (!error)
        {
          error = true;
          next = false;
        }

        var action = await messageService.WaitUserActionAsync();

        if (messageService.ButtonService != null)
        {
          messageService.ButtonService.ShowOnlyExitButton();
        }

        if (action == UserAction.None)
        {
          return result;
        }
        else if (action == UserAction.Retry)
        {
          continue;
        }
        else
        {
          next = true;
          error = false;
          return result;
        }
      }
      while (error);

      return result;
    }


    /// <summary>
    /// Универсальный цикл для выполнения операции с поддержкой повтора, пропуска и завершения.
    /// </summary>
    /// <param name="operation">Функция, возвращающая true при успехе и false при ошибке.</param>
    /// <param name="messageService">Сервис сообщений для ожидания действия пользователя.</param>
    /// <param name="loop">Всегда вызывает повтор, даже если первое измерение без ошибок.</param>
    public static async Task<(bool Connect, string Answer)> GetRunWithUserRepeatAsync(
        Func<Task<(bool Connect, string Answer)>> operation,
        IUserMessageService messageService,
        bool loop = false)
    {
      bool error = loop;
      bool next = !loop;
      bool result = true;
      (bool Connect, string Answer) success;
      do
      {
        success = await operation();
        result = success.Connect;

        if (success.Connect && next)
        {
          return success;
        }
        else if (!error)
        {
          error = true;
          next = false;
        }

        if (messageService != null)
        {
          var action = await messageService.WaitUserActionAsync();

          if (messageService.ButtonService != null)
          {
            messageService.ButtonService.ShowOnlyExitButton();
          }


          if (action == UserAction.None)
          {
            return success;
          }
          else if (action == UserAction.Retry)
          {
            continue;
          }
          else
          {
            next = true;
            error = false;
            if (messageService.ButtonService != null)
            {
              messageService.ButtonService.ShowOnlyStopAndFinishButtons();
            }
            return success;
          }
        }
        else
        {
          break;
        }
      }
      while (error);

      return success;
    }
  }
}
