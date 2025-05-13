using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConsoleUI.ConsoleCommanding.Engine;
using ConsoleUI.ConsoleCommanding.Services;
using ConsoleUI.ConsoleLogic;

namespace ConsoleUI.ConsoleUI
{
  public partial class ConsoleOverlay : Window
  {
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private List<string> _commandSuggestions = new();
    private int _autocompleteIndex = -1;

    private readonly CommandHandler _handler;
    private readonly ConsoleManager _manager;
    private TaskCompletionSource<string> _readLineTcs;


    public ConsoleOverlay()
    {
      InitializeComponent();

      // Инициализация консольной логики
      var writer = new ConsoleWriterAdapter();
      var factory = new CommandFactory();
      var commands = factory.CreateAll(writer);
      _handler = new CommandHandler(commands);
      ConsoleTextManager.Instance.Append("[DEBUG] Handler инициализирован, команд: " + commands.Count);
      _manager = new ConsoleManager(writer, _handler);

      // Подписка на вывод
      ConsoleTextManager.Instance.Subscribe(AppendLogEntry);
      Loaded += (_, _) => CommandInput.Focus();
    }

    private void AppendLogEntry(LogEntry entry)
    {
      var block = new TextBlock
      {
        Text = entry.Text,
        Foreground = entry.Color,
        FontFamily = new System.Windows.Media.FontFamily("Consolas"),
        FontSize = 14,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 2, 0, 2)
      };

      ConsolePanel.Children.Add(block);
      ConsoleScroll.ScrollToEnd();
    }

    private async void CommandInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        string input = CommandInput.Text.Trim();

        if (_readLineTcs != null)
        {
          CommandInput.Clear();
          CommandInput.IsReadOnly = false;
          _readLineTcs.TrySetResult(input);
          _readLineTcs = null;
          return;
        }

        if (!string.IsNullOrEmpty(input))
        {
          ConsoleTextManager.Instance.Append($"> {input}");
          _commandHistory.Add(input);
          _historyIndex = _commandHistory.Count;

          CommandInput.Clear();
          await _manager.RunCommandAsync(input);
          CommandInput.Clear();
        }

        e.Handled = true;
      }

      else if (e.Key == Key.Up)
      {
        if (_commandHistory.Count == 0) return;
        _historyIndex = Math.Max(0, _historyIndex - 1);
        CommandInput.Text = _commandHistory[_historyIndex];
        CommandInput.SelectionStart = CommandInput.Text.Length;
        e.Handled = true;
      }

      else if (e.Key == Key.Down)
      {
        if (_commandHistory.Count == 0) return;
        _historyIndex = Math.Min(_commandHistory.Count, _historyIndex + 1);

        if (_historyIndex < _commandHistory.Count)
          CommandInput.Text = _commandHistory[_historyIndex];
        else
          CommandInput.Clear();

        CommandInput.SelectionStart = CommandInput.Text.Length;
        e.Handled = true;
      }

      else if (e.Key == Key.Tab)
      {
        e.Handled = true;

        if (_commandSuggestions.Count == 1)
        {
          CommandInput.Text = _commandSuggestions[0];
          CommandInput.SelectionStart = CommandInput.Text.Length;
          AutocompleteBox.Visibility = Visibility.Collapsed;
        }
        else if (_commandSuggestions.Count > 1)
        {
          AutocompleteBox.ItemsSource = _commandSuggestions;
          AutocompleteBox.Visibility = Visibility.Visible;
          AutocompleteBox.SelectedIndex = 0;
        }
      }

      else if (e.Key == Key.Down && AutocompleteBox.Visibility == Visibility.Visible)
      {
        AutocompleteBox.Focus();
        AutocompleteBox.SelectedIndex = 0;
        e.Handled = true;
      }
    }

    private void CommandInput_TextChanged(object sender, TextChangedEventArgs e)
    {
      var text = CommandInput.Text.Trim();
      if (string.IsNullOrWhiteSpace(text))
      {
        AutocompleteBox.Visibility = Visibility.Collapsed;
        return;
      }

      _commandSuggestions = _handler.GetAllCommandNames()
        .Where(c => c.StartsWith(text, StringComparison.OrdinalIgnoreCase))
        .OrderBy(c => c)
        .ToList();

      if (_commandSuggestions.Count > 0)
      {
        AutocompleteBox.ItemsSource = _commandSuggestions;
        AutocompleteBox.Visibility = Visibility.Visible;
        _autocompleteIndex = -1;
      }
      else
      {
        AutocompleteBox.Visibility = Visibility.Collapsed;
      }
    }

    public void ClearConsoleUI()
    {
      ConsolePanel.Children.Clear();
    }

    private void AutocompleteBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (AutocompleteBox.SelectedItem is string selected)
      {
        CommandInput.Text = selected;
        CommandInput.SelectionStart = selected.Length;
        CommandInput.Focus();
        AutocompleteBox.Visibility = Visibility.Collapsed;
      }
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ButtonState == MouseButtonState.Pressed)
        DragMove();
    }

    public async Task<string> ReadLineAsync()
    {
      _readLineTcs = new TaskCompletionSource<string>();

      await Dispatcher.InvokeAsync(() =>
      {
        CommandInput.IsReadOnly = false;
        CommandInput.Focus();
      });

      return await _readLineTcs.Task;
    }
  }
}
