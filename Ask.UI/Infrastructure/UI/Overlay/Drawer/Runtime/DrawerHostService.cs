using Ask.Core.Contracts.Debugging;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.UI.Features.ExecutionSelection.ViewModels;
using System.Windows;
using System.Windows.Threading;

namespace Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime
{
  public sealed class DrawerHostService
  {
    private static readonly Lazy<DrawerHostService> _instance = new(() => new DrawerHostService());
    private readonly SemaphoreSlim _openGate = new(1, 1);
    private readonly DrawerViewModel _viewModel = new();

    private bool _isInitialized;

    private DrawerHostService()
    {
    }

    public static DrawerHostService Instance => _instance.Value;

    public DrawerViewModel ViewModel => _viewModel;
    public bool IsOpen => _viewModel.IsOpen;

    // Runtime toggle: while drawer is open, block global app hotkeys/handlers.
    public bool BlockGlobalInputWhenOpen { get; set; } = true;

    public bool ShouldBlockGlobalInput => _viewModel.IsOpen && BlockGlobalInputWhenOpen && !_viewModel.IsCustomContent;

    public void EnsureInitialized()
    {
      if (_isInitialized)
      {
        return;
      }

      EventAggregator.Subscribe<OpenCommandDrawerRequest>(OnOpenRequest);
      _isInitialized = true;
    }

    public async Task<BaseCommandModel?> OpenAsync(OpenCommandDrawerRequest request, CancellationToken cancellationToken = default)
    {
      await _openGate.WaitAsync(cancellationToken).ConfigureAwait(false);

      BaseCommandModel? selectedCommand = null;
      var tcs = new TaskCompletionSource<BaseCommandModel?>(TaskCreationOptions.RunContinuationsAsynchronously);

      try
      {
        await RunOnUiThreadAsync(() =>
        {
          _viewModel.Open(request.Commands, request.BreakpointCommand, result => tcs.TrySetResult(result));
        }).ConfigureAwait(false);

        using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
        {
          selectedCommand = await tcs.Task.ConfigureAwait(false);
        }
      }
      catch (TaskCanceledException)
      {
        selectedCommand = null;
      }
      finally
      {
        CommandDrawerEventAdapter.RaiseResult(request.RequestId, selectedCommand);
        _openGate.Release();
      }

      return selectedCommand;
    }

    public Task OpenContentAsync(object content, string title, string subtitle, Action? onClose = null, double panelWidth = 900d)
    {
      return RunOnUiThreadAsync(() =>
      {
        _viewModel.OpenContent(content, title, subtitle, onClose, panelWidth);
      });
    }

    public void Close()
    {
      _viewModel.Cancel();
    }

    private void OnOpenRequest(OpenCommandDrawerRequest request)
    {
      _ = OpenAsync(request);
    }

    private static Task RunOnUiThreadAsync(Action action)
    {
      var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
      if (dispatcher.CheckAccess())
      {
        action();
        return Task.CompletedTask;
      }

      return dispatcher.InvokeAsync(action).Task;
    }
  }
}

