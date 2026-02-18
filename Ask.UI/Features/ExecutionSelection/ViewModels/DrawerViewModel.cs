using Ask.Core.Shared.DTO.Executor;
using Ask.UI.Shared.Commands;
using Ask.UI.Shared.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Ask.UI.Features.ExecutionSelection.ViewModels
{
  public sealed class DrawerViewModel : ObservableObject
  {
    private bool _isOpen;
    private CommandPreviewViewModel? _selectedCommand;
    private Action<BaseCommandModel?>? _onComplete;

    public ObservableCollection<CommandPreviewViewModel> Commands { get; } = new();

    public bool IsOpen
    {
      get => _isOpen;
      private set => SetProperty(ref _isOpen, value);
    }

    public CommandPreviewViewModel? SelectedCommand
    {
      get => _selectedCommand;
      set
      {
        if (SetProperty(ref _selectedCommand, value))
        {
          _confirmCommand.RaiseCanExecuteChanged();
        }
      }
    }

    public string SelectedCommandText => SelectedCommand?.FullCommandText ?? string.Empty;

    private readonly RelayCommand _confirmCommand;
    private readonly RelayCommand _cancelCommand;

    public ICommand ConfirmCommand => _confirmCommand;
    public ICommand CancelCommand => _cancelCommand;

    public DrawerViewModel()
    {
      _confirmCommand = new RelayCommand(ConfirmSelection, () => SelectedCommand != null);
      _cancelCommand = new RelayCommand(Cancel);
      PropertyChanged += (_, e) =>
      {
        if (e.PropertyName == nameof(SelectedCommand))
        {
          RaisePropertyChanged(nameof(SelectedCommandText));
        }
      };
    }

    public void Open(IReadOnlyList<BaseCommandModel> commands, BaseCommandModel breakpointCommand, Action<BaseCommandModel?> onComplete)
    {
      Commands.Clear();

      foreach (var command in commands)
      {
        Commands.Add(new CommandPreviewViewModel(command));
      }

      _onComplete = onComplete;
      SelectedCommand = Commands.FirstOrDefault(x => ReferenceEquals(x.Command, breakpointCommand)) ?? Commands.FirstOrDefault();
      IsOpen = true;
    }

    public void ConfirmSelection()
    {
      if (!IsOpen)
      {
        return;
      }

      Close(SelectedCommand?.Command);
    }

    public void Cancel()
    {
      if (!IsOpen)
      {
        return;
      }

      Close(null);
    }

    private void Close(BaseCommandModel? selectedCommand)
    {
      IsOpen = false;
      var callback = _onComplete;
      _onComplete = null;
      callback?.Invoke(selectedCommand);
    }
  }
}
