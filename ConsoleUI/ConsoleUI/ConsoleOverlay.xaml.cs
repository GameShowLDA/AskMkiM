using ConsoleUI.ConsoleCommanding.Engine;
using ConsoleUI.ConsoleCommanding.Services;
using ConsoleUI.ConsoleLogic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ConsoleUI.ConsoleUI
{
  public partial class ConsoleOverlay : Window
  {
    private const int MaxUiEntries = ConsoleTextManager.MaxBufferSize;

    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private List<string> _commandSuggestions = new();
    private int _autocompleteIndex = -1;

    private readonly CommandHandler _handler;
    private readonly ConsoleManager _manager;
    private TaskCompletionSource<string> _readLineTcs;

    private bool _isPasswordMode = false;
    private StringBuilder _passwordBuffer = new();

    private bool _splitViewEnabled = false;
    private Grid? _splitRoot;
    private StackPanel? _uiCol;
    private StackPanel? _devCol;

    public ConsoleOverlay() : this(false) { }

    public ConsoleOverlay(bool startSplit)
    {
      InitializeComponent();

      var writer = new ConsoleWriterAdapter();
      var factory = new CommandFactory();
      var commands = factory.CreateAll(writer);
      _handler = new CommandHandler(commands);
      ConsoleTextManager.Instance.Append("[DEBUG] Handler инициализирован, команд: " + commands.Count);
      _manager = new ConsoleManager(writer, _handler);

      if (startSplit)
      {
        _splitViewEnabled = true;
        BuildSplitViewIfNeeded();
      }
      ConsoleTextManager.Instance.Subscribe(AppendLogEntry);
      Loaded += (_, _) => CommandInput.Focus();
      Closed += (_, _) => ConsoleTextManager.Instance.Unsubscribe(AppendLogEntry);

      this.PreviewKeyDown += (_, e) =>
      {
        if (e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
          e.Handled = true;
          Close();
        }
      };
    }

    private void AppendLogEntry(LogEntry entry)
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(() => AppendLogEntry(entry));
        return;
      }

      if (_splitViewEnabled)
      {
        BuildSplitViewIfNeeded();

        var block = new TextBlock
        {
          Text = entry.Text,
          Foreground = entry.Color,
          FontFamily = new FontFamily("Consolas"),
          FontSize = 14,
          TextWrapping = TextWrapping.Wrap,
          Margin = new Thickness(0, 2, 0, 2)
        };

        if (TryClassify(entry, out bool isDevice))
        {
          if (isDevice)
          {
            _devCol!.Children.Add(block);
            TrimPanel(_devCol);
          }
          else
          {
            _uiCol!.Children.Add(block);
            TrimPanel(_uiCol);
          }
        }
        else
        {
          block.Opacity = 0.8;
          _uiCol!.Children.Add(block);
          TrimPanel(_uiCol);
        }

        ConsoleScroll.ScrollToEnd();
        return;
      }
      var normal = new TextBlock
      {
        Text = entry.Text,
        Foreground = entry.Color,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 14,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 2, 0, 2)
      };

      ConsolePanel.Children.Add(normal);
      TrimPanel(ConsolePanel);
      ConsoleScroll.ScrollToEnd();
    }

    private async void CommandInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        string input = _isPasswordMode ? _passwordBuffer.ToString() : CommandInput.Text.Trim();

        if (_readLineTcs != null)
        {

          CommandInput.Clear();
          CommandInput.IsReadOnly = false;
          _isPasswordMode = false;

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

    private static bool TryClassify(string text, out bool isDevice)
    {
      var t = text ?? string.Empty;

      if (t.Contains("_Device", StringComparison.OrdinalIgnoreCase) ||
          t.Contains("[DEVICE]", StringComparison.OrdinalIgnoreCase) ||
          Regex.IsMatch(t, @"(^|[\s;\[\]])Device([;\]\s]|$)", RegexOptions.IgnoreCase) ||
          Regex.IsMatch(t, @"logger\s*=\s*.+_Device", RegexOptions.IgnoreCase))
      { isDevice = true; return true; }

      if (t.Contains("_UI", StringComparison.OrdinalIgnoreCase) ||
          t.Contains("[UI]", StringComparison.OrdinalIgnoreCase) ||
          Regex.IsMatch(t, @"(^|[\s;\[\]])UI([;\]\s]|$)", RegexOptions.IgnoreCase) ||
          Regex.IsMatch(t, @"logger\s*=\s*.+_UI", RegexOptions.IgnoreCase))
      { isDevice = false; return true; }

      isDevice = false;
      return false;
    }

    private static bool TryClassify(LogEntry e, out bool isDevice) =>
      TryClassify(e?.Text ?? string.Empty, out isDevice);


    private void BuildSplitViewIfNeeded()
    {
      if (_splitRoot != null) return;

      ConsolePanel.Children.Clear();

      _splitRoot = new Grid { Margin = new Thickness(0) };
      _splitRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      _splitRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
      _splitRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      _splitRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      _splitRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var uiHeader = new TextBlock
      {
        Text = "UI LOGS",
        FontFamily = new FontFamily("Consolas"),
        FontSize = 16,
        FontWeight = FontWeights.Bold,
        Foreground = Brushes.DodgerBlue,
        Margin = new Thickness(0, 0, 0, 6),
        TextAlignment = TextAlignment.Center
      };
      Grid.SetRow(uiHeader, 0); Grid.SetColumn(uiHeader, 0);

      var devHeader = new TextBlock
      {
        Text = "DEVICE LOGS",
        FontFamily = new FontFamily("Consolas"),
        FontSize = 16,
        FontWeight = FontWeights.Bold,
        Foreground = Brushes.OrangeRed,
        Margin = new Thickness(0, 0, 0, 6),
        TextAlignment = TextAlignment.Center
      };
      Grid.SetRow(devHeader, 0); Grid.SetColumn(devHeader, 2);

      var divider = new Border
      {
        Width = 1,
        Background = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
        Margin = new Thickness(8, 0, 8, 0)
      };
      Grid.SetRow(divider, 0); Grid.SetRowSpan(divider, 2); Grid.SetColumn(divider, 1);

      _uiCol = new StackPanel { Orientation = Orientation.Vertical };
      _devCol = new StackPanel { Orientation = Orientation.Vertical };
      Grid.SetRow(_uiCol, 1); Grid.SetColumn(_uiCol, 0);
      Grid.SetRow(_devCol, 1); Grid.SetColumn(_devCol, 2);

      _splitRoot.Children.Add(uiHeader);
      _splitRoot.Children.Add(divider);
      _splitRoot.Children.Add(devHeader);
      _splitRoot.Children.Add(_uiCol);
      _splitRoot.Children.Add(_devCol);

      ConsolePanel.Children.Add(_splitRoot);
    }

    private static void TrimPanel(StackPanel panel)
    {
      while (panel.Children.Count > MaxUiEntries)
        panel.Children.RemoveAt(0);
    }

    private void CommandInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (_isPasswordMode)
      {
        e.Handled = true;

        string clean = new string(e.Text.Where(char.IsLetterOrDigit).ToArray());

        _passwordBuffer.Append(clean);
        CommandInput.Text += new string('*', clean.Length);
        CommandInput.SelectionStart = CommandInput.Text.Length;
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
      if (_splitViewEnabled && _uiCol != null && _devCol != null)
      {
        _uiCol.Children.Clear();
        _devCol.Children.Clear();
        return;
      }
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

    public void SetPasswordMode(bool enabled)
    {
      _isPasswordMode = enabled;
      _passwordBuffer.Clear();

      CommandInput.Clear();
      CommandInput.Focus();
    }
  }
}
