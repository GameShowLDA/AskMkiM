using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using System.Runtime.ExceptionServices;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Core.Services.UI
{
  /// <summary>
  /// Выполняет операции с интерактивным выбором повтора, продолжения или завершения.
  /// </summary>
  public static class UserActionHelper
  {
    /// <summary>
    /// Выполняет логическую операцию с поддержкой пользовательского повтора.
    /// </summary>
    /// <param name="operation">Асинхронная операция.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="loop">Признак обязательного интерактивного режима.</param>
    /// <param name="deviceTask">Признак аппаратной операции.</param>
    public static async Task RunWithUserRepeatAsync(
      Func<Task<bool>> operation,
      IUserInteractionService? messageService,
      bool loop = false,
      bool deviceTask = false)
    {
      await RunCoreAsync(
        operation,
        static result => result,
        messageService,
        loop,
        deviceTask,
        exceptionFallback: null);
    }

    /// <summary>
    /// Выполняет логическую операцию с поддержкой пользовательского повтора.
    /// </summary>
    /// <param name="operation">Асинхронная операция.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="loop">Признак обязательного интерактивного режима.</param>
    /// <param name="deviceTask">Признак аппаратной операции.</param>
    /// <returns>Результат последней подтверждённой попытки.</returns>
    public static Task<bool> GetRunWithUserRepeatAsync(
      Func<Task<bool>> operation,
      IUserInteractionService? messageService,
      bool loop = false,
      bool deviceTask = false)
    {
      return RunCoreAsync(
        operation,
        static result => result,
        messageService,
        loop,
        deviceTask,
        exceptionFallback: null);
    }

    /// <summary>
    /// Выполняет операцию подключения с поддержкой пользовательского повтора.
    /// </summary>
    /// <param name="operation">Асинхронная операция подключения.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="loop">Признак обязательного интерактивного режима.</param>
    /// <param name="deviceTask">Признак аппаратной операции.</param>
    /// <returns>Результат последней подтверждённой попытки подключения.</returns>
    public static Task<(bool Connect, string Answer)> GetRunWithUserRepeatAsync(
      Func<Task<(bool Connect, string Answer)>> operation,
      IUserInteractionService? messageService,
      bool loop = false,
      bool deviceTask = false)
    {
      return RunCoreAsync(
        operation,
        static result => result.Item1,
        messageService,
        loop,
        deviceTask,
        exception => (false, exception.Message));
    }

    /// <summary>
    /// Выполняет измерительную операцию с поддержкой пользовательского повтора.
    /// </summary>
    /// <param name="operation">Асинхронная измерительная операция.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="loop">Признак обязательного интерактивного режима.</param>
    /// <param name="deviceTask">Признак аппаратной операции.</param>
    /// <returns>Результат последней подтверждённой попытки измерения.</returns>
    public static Task<(bool Connect, double Answer)> GetRunWithUserRepeatAsync(
      Func<Task<(bool Connect, double Answer)>> operation,
      IUserInteractionService? messageService,
      bool loop = false,
      bool deviceTask = false)
    {
      return RunCoreAsync(
        operation,
        static result => result.Item1,
        messageService,
        loop,
        deviceTask,
        static _ => (false, -1));
    }

    /// <summary>
    /// Выполняет типизированную операцию с поддержкой пользовательского повтора.
    /// </summary>
    /// <typeparam name="T">Тип результата операции.</typeparam>
    /// <param name="operation">Асинхронная операция.</param>
    /// <param name="isSuccessful">Проверка успешности результата.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="loop">Признак обязательного интерактивного режима.</param>
    /// <param name="deviceTask">Признак аппаратной операции.</param>
    /// <returns>Результат последней подтверждённой попытки.</returns>
    public static Task<T> GetRunWithUserRepeatAsync<T>(
      Func<Task<T>> operation,
      Func<T, bool> isSuccessful,
      IUserInteractionService? messageService,
      bool loop = false,
      bool deviceTask = false)
    {
      ArgumentNullException.ThrowIfNull(isSuccessful);

      return RunCoreAsync(
        operation,
        isSuccessful,
        messageService,
        loop,
        deviceTask,
        exceptionFallback: null);
    }

    private static async Task<T> RunCoreAsync<T>(
      Func<Task<T>> operation,
      Func<T, bool> isSuccessful,
      IUserInteractionService? messageService,
      bool loop,
      bool deviceTask,
      Func<Exception, T>? exceptionFallback)
    {
      ArgumentNullException.ThrowIfNull(operation);
      ArgumentNullException.ThrowIfNull(isSuccessful);

      bool interactiveMode = loop;
      int attempt = 0;

      while (true)
      {
        attempt++;
        T result = default!;
        Exception? hardwareException = null;
        bool operationSucceeded = false;

        try
        {
          messageService?.GetCancellationToken().ThrowIfCancellationRequested();
          result = await operation();
          operationSucceeded = isSuccessful(result);
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (Exception ex)
        {
          hardwareException = ex;
          LogException($"Аппаратная операция завершилась ошибкой на попытке {attempt}.", ex, isDeviceLog: true);
        }

        if (EquipmentExecutionContext.IsMandatoryFinalization)
        {
          return ResolveWithoutInteraction(result, hardwareException, exceptionFallback);
        }

        bool hardwareSucceeded = hardwareException == null && (!deviceTask || operationSucceeded);
        if (!interactiveMode && hardwareSucceeded && operationSucceeded)
        {
          return result;
        }

        if (messageService == null)
        {
          return ResolveWithoutInteraction(result, hardwareException, exceptionFallback);
        }

        bool forceInteraction = interactiveMode;
        interactiveMode = true;
        UserAction action = await messageService.WaitUserActionAsync(
          loop: loop || forceInteraction,
          deviceTask: !hardwareSucceeded || deviceTask,
          canContinue: hardwareSucceeded);

        switch (action)
        {
          case UserAction.Retry:
            LogInformation($"Оператор запросил повтор операции после попытки {attempt}.", isDeviceLog: true);
            continue;

          case UserAction.Continue when hardwareSucceeded:
            messageService.ButtonService?.ShowOnlyStopAndFinishButtons();
            return result;

          case UserAction.Abort:
            throw new OperationCanceledException(
              "Выполнение завершено оператором.",
              messageService.GetCancellationToken());

          case UserAction.None:
            return ResolveWithoutInteraction(result, hardwareException, exceptionFallback);

          default:
            continue;
        }
      }
    }

    private static T ResolveWithoutInteraction<T>(
      T result,
      Exception? exception,
      Func<Exception, T>? exceptionFallback)
    {
      if (exception == null)
      {
        return result;
      }

      if (exceptionFallback != null)
      {
        return exceptionFallback(exception);
      }

      ExceptionDispatchInfo.Capture(exception).Throw();
      return default!;
    }
  }
}
