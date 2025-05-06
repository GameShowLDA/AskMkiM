using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Utilities.Models;
using static AppConfiguration.Base.EventAggregator;
using static AppConfiguration.Execution.ExecutionConfig;
using static AppConfiguration.Protocol.ProtocolConfig;
using static AppConfiguration.SystemState.SystemStateManager;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace UI.Controls.ProtocolController
{
  public class ProtocolExecutionRunner
  {
    private readonly Protocol _protocol;
    private readonly Stopwatch _stopwatch = new();
    private Task _processTask;
    private CancellationTokenSource _cts;
    private bool _isFinalized;

    public ProtocolPauseManager PauseManager { get; } = new();

    public static event Action<bool> StartProcessing;

    public TaskCompletionSource<bool> PauseCompletionSource { get; private set; }

    public bool IsRunning => _processTask != null && !_processTask.IsCompleted;

    public ProtocolExecutionRunner(Protocol protocol)
    {
      _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
    }

    public async Task StartAsync(ProtocolExecutionSettings settings, string processName)
    {
      if (IsRunning)
      {
        LogError($"Попытка повторного запуска \"{processName}\" во время активного процесса.");
        return;
      }

      await _protocol.Message.ClearAllAsync();
      _isFinalized = false;

      // TODO: Раскомментировать при необходимости контроля питания системы
      // if (!await EnsureSystemPowerAvailable())
      // {
      //   await ReportAndExit("Нет подключения к системе. Пожалуйста, подключитесь и повторите попытку.");
      //   return;
      // }

      if (!await RunPreActionAsync(settings.PreActionDelegate))
      {
        await ReportAndExit("Ошибка предварительной подготовки.");
        return;
      }

      if (settings.StartDelegate == null)
      {
        await ReportAndExit("Системная ошибка: не определён делегат запуска.");
        LogError("StartDelegate не задан");
        return;
      }

      _protocol.ButtonManager.ShowOnlyStopAndFinishButtons();
      StartProcessing?.Invoke(true);

      await PrepareStartAsync(processName);

      if (!await GetIsIdleModeEnabled())
        await ResetSystemAsync();

      await ExecuteAsync(settings, processName);
    }

    public void Cancel()
    {
      if (_cts is { IsCancellationRequested: false })
        _cts.Cancel();

      PauseCompletionSource?.TrySetResult(true);
      PauseManager.ReleasePause(); // гарантированно снимаем паузу
    }

    private async Task ExecuteAsync(ProtocolExecutionSettings settings, string name)
    {
      _cts?.Dispose();
      _cts = new CancellationTokenSource();
      PauseCompletionSource = new TaskCompletionSource<bool>();

      try
      {
        _stopwatch.Restart();

        var interceptor = new ProtocolExecutionInterceptor(PauseManager, _cts.Token, _protocol);

        _processTask = Task.Run(async () =>
        {
          try
          {
            await interceptor.Run(async token =>
            {
              await settings.StartDelegate(token);
            });
          }
          catch (OperationCanceledException)
          {
            // задача отменена, ничего не делаем
          }
        }, _cts.Token);

        await SetIsLocked(true);
        await _processTask;

        if (settings.StopDelegate != null)
        {
          LogInformation($"Выполняется StopDelegate для \"{name}\"");
          await settings.StopDelegate(CancellationToken.None); // Без токена отмены!
        }

        await FinalizeAsync(null, name);
      }
      catch (OperationCanceledException)
      {
        LogInformation($"Процесс \"{name}\" был отменён пользователем.");
        if (settings.StopDelegate != null)
          await settings.StopDelegate(CancellationToken.None);
        await FinalizeAsync(null, name);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка выполнения \"{name}\"", ex);
        if (settings.StopDelegate != null)
          await settings.StopDelegate(CancellationToken.None);
        await FinalizeAsync(null, name);
      }
      finally
      {
        _stopwatch.Stop();
      }
    }

    internal async Task FinalizeAsync(StopDelegate stopDelegate = null, string name = null)
    {
      if (_isFinalized)
        return;

      _isFinalized = true;

      _protocol.ButtonManager.ShowOnlyStartButton();

      LogInformation($"Завершение \"{name}\"");

      Cancel();

      if (_processTask != null)
      {
        try
        {
          await _processTask;
        }
        catch (Exception ex)
        {
          LogException($"Ошибка ожидания завершения \"{name}\"", ex);
        }
        finally
        {
          _processTask.Dispose();
          _processTask = null;
        }
      }

      ResetState();
      await ResetSystemAsync();
      await HandleProtocolActionsAsync();
      await DisplayCompletionMessage();
      StartProcessing?.Invoke(false);
    }

    private async Task<bool> EnsureSystemPowerAvailable()
    {
      return await GetIsIdleModeEnabled() || await GetIsActivePower();
    }

    private async Task<bool> RunPreActionAsync(PreActionDelegate preAction)
    {
      if (preAction == null)
        return true;

      try
      {
        await preAction(_cts.Token);
        return true;
      }
      catch (Exception ex)
      {
        LogException("Ошибка предварительного действия", ex);
        return false;
      }
    }

    private async Task PrepareStartAsync(string name)
    {
      LogInformation($"Старт процесса \"{name}\"");
      if (await GetTimeStart())
        _stopwatch.Restart();
    }

    private async Task ResetSystemAsync()
    {
      await Application.Current.Dispatcher.Invoke(async () =>
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
        await SetIsLocked(false);
        if (await GetTimeStart())
          _stopwatch.Stop();

        RaiseInfoMessage("");
      });
    }

    private async Task ReportAndExit(string message)
    {
      await _protocol.Message.AppendLineAsync(new ShowMessageModel(message, ErrorMessage.TitleColor));
      await FinalizeAsync();
    }

    private void ResetState()
    {
      _cts?.Dispose();
      _cts = null;
      PauseCompletionSource = null;
      _stopwatch.Stop();
    }

    private async Task HandleProtocolActionsAsync()
    {
      await _protocol.Message.AppendLineAsync(new ShowMessageModel("Процесс завершён", SuccessMessage.TitleColor));
    }

    private async Task DisplayCompletionMessage()
    {
      if (await GetTimeStart())
      {
        string time = _stopwatch.Elapsed.ToString(@"mm\:ss\.fff");
        await _protocol.Message.AppendLineAsync(new ShowMessageModel($"Время выполнения: {time}", SuccessMessage.TitleColor));
      }
    }
  }
}
