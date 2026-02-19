using Ask.Core.Shared.DTO.Executor;
using Ask.UI.Shared.Commands;
using Ask.UI.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Ask.UI.Features.ExecutionSelection.ViewModels
{
  public sealed class DrawerViewModel : ObservableObject
  {
    private static readonly string[] ExcludedMnemonicPrefixes =
    {
      "OK",
      "SP",
      "RM",
      "VSH",
      "\u041E\u041A",
      "\u0421\u041F",
      "\u0420\u041C",
      "\u0412\u0428"
    };

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
        if (IsSameCommand(command, breakpointCommand))
        {
          continue;
        }

        if (IsExcludedMnemonic(command.Mnemonic))
        {
          continue;
        }

        Commands.Add(new CommandPreviewViewModel(command));
      }

      _onComplete = onComplete;
      SelectedCommand = Commands.FirstOrDefault();
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

    private static bool IsExcludedMnemonic(string? mnemonic)
    {
      if (string.IsNullOrWhiteSpace(mnemonic))
      {
        return false;
      }

      var normalized = NormalizeMnemonic(mnemonic);
      if (string.IsNullOrEmpty(normalized))
      {
        return false;
      }

      foreach (var prefix in ExcludedMnemonicPrefixes)
      {
        var normalizedPrefix = NormalizeMnemonic(prefix);
        if (normalized.StartsWith(normalizedPrefix, StringComparison.Ordinal))
        {
          return true;
        }
      }

      return false;
    }

    private static string NormalizeMnemonic(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return string.Empty;
      }

      var chars = value
        .Trim()
        .ToUpperInvariant()
        .Where(char.IsLetterOrDigit);

      return new string(chars.ToArray());
    }

    private static bool IsSameCommand(BaseCommandModel source, BaseCommandModel target)
    {
      if (ReferenceEquals(source, target))
      {
        return true;
      }

      return string.Equals(source.CommandNumber?.Trim(), target.CommandNumber?.Trim(), StringComparison.OrdinalIgnoreCase)
             && string.Equals(source.Mnemonic?.Trim(), target.Mnemonic?.Trim(), StringComparison.OrdinalIgnoreCase)
             && string.Equals(source.CommandBody?.Trim(), target.CommandBody?.Trim(), StringComparison.Ordinal);
    }
  }
}
