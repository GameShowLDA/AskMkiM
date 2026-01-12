using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;

namespace Ask.Core.Services.UI
{
  /// <summary>
  /// Служебный класс для выполнения операций с поддержкой пользовательского выбора:
  /// повторить, завершить или пропустить.
  /// </summary>
  public static class UserActionHelper
  {
    /// <summary>
    /// Выполняет операцию с поддержкой повтора, завершения и пропуска.
    /// Завершается после первого успешного выполнения, если <paramref name="loop"/> не включён.
    /// </summary>
    /// <param name="operation">Асинхронная операция, возвращающая true при успехе.</param>
    /// <param name="messageService">Сервис пользовательских сообщений.</param>
    /// <param name="loop">Если true — выполняет цикл хотя бы один раз независимо от результата.</param>
    public static async Task RunWithUserRepeatAsync(
        Func<Task<bool>> operation,
        IUserInteractionService messageService,
        bool loop = false,
        bool deviceTask = false)
    {
      await RunCoreAsync(operation, messageService, loop, deviceTask);
    }

    /// <summary>
    /// Выполняет операцию с поддержкой пользовательского выбора и возвращает её результат.
    /// </summary>
    /// <param name="operation">Асинхронная операция, возвращающая true при успехе.</param>
    /// <param name="messageService">Сервис пользовательских сообщений.</param>
    /// <param name="loop">Если true — выполняет хотя бы один цикл независимо от результата.</param>
    /// <returns>True, если операция была успешной.</returns>
    public static async Task<bool> GetRunWithUserRepeatAsync(
        Func<Task<bool>> operation,
        IUserInteractionService messageService,
        bool loop = false,
        bool deviceTask = false)
    {
      var result = await RunCoreAsync(operation, messageService, loop, deviceTask);
      return result.Success;
    }

    /// <summary>
    /// Выполняет операцию, возвращающую результат подключения и строку ответа, с поддержкой пользовательского выбора.
    /// </summary>
    /// <param name="operation">Асинхронная операция, возвращающая результат подключения и строку ответа.</param>
    /// <param name="messageService">Сервис пользовательских сообщений.</param>
    /// <param name="loop">Если true — выполняет хотя бы один цикл независимо от результата.</param>
    /// <returns>Кортеж (успех подключения, строка ответа).</returns>
    public static async Task<(bool Connect, string Answer)> GetRunWithUserRepeatAsync(
        Func<Task<(bool Connect, string Answer)>> operation,
        IUserInteractionService messageService,
        bool loop = false,
        bool deviceTask = false)
    {
      bool error = loop;
      bool next = !loop;
      (bool Connect, string Answer) result;

      do
      {
        result = await operation();

        if (result.Connect && next)
          return result;

        if (!error)
        {
          error = true;
          next = false;
        }

        if (messageService == null)
          break;

        var action = await messageService.WaitUserActionAsync(loop, deviceTask);
        ApplyButtonMode(messageService, onlyExit: true);

        if (action == UserAction.None)
          return result;

        if (action == UserAction.Retry)
          continue;

        next = true;
        error = false;
        ApplyButtonMode(messageService, onlyExit: false);
        return result;

      }
      while (error);

      return result;
    }

    /// <summary>
    /// Выполняет операцию, возвращающую результат подключения и строку ответа, с поддержкой пользовательского выбора.
    /// </summary>
    /// <param name="operation">Асинхронная операция, возвращающая результат подключения и строку ответа.</param>
    /// <param name="messageService">Сервис пользовательских сообщений.</param>
    /// <param name="loop">Если true — выполняет хотя бы один цикл независимо от результата.</param>
    /// <returns>Кортеж (успех подключения, строка ответа).</returns>
    public static async Task<(bool Connect, double Answer)> GetRunWithUserRepeatAsync(
        Func<Task<(bool Connect, double Answer)>> operation,
        IUserInteractionService messageService,
        bool loop = false,
        bool deviceTask = false)
    {
      bool error = loop;
      bool next = !loop;
      (bool Connect, double Answer) result;

      do
      {
        result = await operation();

        if (result.Connect && next)
          return result;

        if (!error)
        {
          error = true;
          next = false;
        }

        if (messageService == null)
          break;

        var action = await messageService.WaitUserActionAsync(loop, deviceTask);
        ApplyButtonMode(messageService, onlyExit: true);

        if (action == UserAction.None)
          return result;

        if (action == UserAction.Retry)
          continue;

        next = true;
        error = false;
        ApplyButtonMode(messageService, onlyExit: false);
        return result;

      }
      while (error);

      return result;
    }


    /// <summary>
    /// Внутренняя логика повтора операции, общая для методов с типом bool.
    /// </summary>
    /// <param name="operation">Операция, возвращающая true при успехе.</param>
    /// <param name="messageService">Сервис пользовательских сообщений.</param>
    /// <param name="loop">Принудительный запуск повторов независимо от результата.</param>
    /// <returns>Кортеж (успешность операции, текстовое сообщение).</returns>
    private static async Task<(bool Success, string Message)> RunCoreAsync(
        Func<Task<bool>> operation,
        IUserInteractionService messageService,
        bool loop, bool deviceTask)
    {
      bool error = loop;
      bool next = !loop;

      do
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        bool success = await operation();

        if (success && next)
          return (true, string.Empty);

        if (!error)
        {
          error = true;
          next = false;
        }

        var action = await messageService.WaitUserActionAsync(loop, deviceTask);
        ApplyButtonMode(messageService, onlyExit: true);

        if (action == UserAction.None)
        {
          ApplyButtonMode(messageService, onlyExit: false);
          return (success, string.Empty);
        }

        if (action == UserAction.Retry)
          continue;

        next = true;
        error = false;
        ApplyButtonMode(messageService, onlyExit: false);
        return (success, string.Empty);

      } while (error);

      ApplyButtonMode(messageService, onlyExit: false);
      return (false, string.Empty);
    }

    /// <summary>
    /// Устанавливает режим отображения кнопок в пользовательском интерфейсе.
    /// </summary>
    /// <param name="messageService">Сервис пользовательских сообщений.</param>
    /// <param name="onlyExit">Если true — отображается только кнопка "Завершить", иначе — "Пауза" и "Завершить".</param>
    private static void ApplyButtonMode(IUserInteractionService messageService, bool onlyExit)
    {
      if (messageService?.ButtonService == null)
        return;

      if (onlyExit)
        messageService.ButtonService.ShowOnlyExitButton();
      else
        messageService.ButtonService.ShowOnlyStopAndFinishButtons();
    }
  }
}
