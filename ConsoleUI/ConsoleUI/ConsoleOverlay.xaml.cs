using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConsoleUI.ConsoleCommanding.Commands;
using ConsoleUI.ConsoleCommanding;
using ConsoleUI.ConsoleLogic;

namespace ConsoleUI.ConsoleUI
{
  public partial class ConsoleOverlay : Window
  {
    public ConsoleOverlay()
    {
      InitializeComponent();

      ConsoleTextManager.Instance.Subscribe(AppendLogEntry);
    }

    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private List<string> _commandSuggestions = new();
    private int _autocompleteIndex = -1;

    private readonly CommandRouter _router = new CommandRouter(new ConsoleCommanding.ICommand[]
    {
      new HelpCommand(new ConsoleCommanding.ICommand[] {
        new HelpCommand(null), new EchoCommand(), new ClearCommand()
      }),
      new EchoCommand(),
      new ClearCommand()
    });
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

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ButtonState == MouseButtonState.Pressed)
        DragMove();
    }

    private void CommandInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        string input = CommandInput.Text.Trim();
        if (!string.IsNullOrEmpty(input))
        {
          ConsoleTextManager.Instance.Append($"> {input}");
          _commandHistory.Add(input);
          _historyIndex = _commandHistory.Count;

          var context = new CommandContext(ConsoleTextManager.Instance.Append);
          _ = _router.RouteAsync(input, context);

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
          AutocompleteBox.Focus();
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

      _commandSuggestions = _router.GetCommandNames()
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

  }
}
