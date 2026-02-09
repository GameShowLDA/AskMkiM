using ConsoleUI.ConsoleCommanding.Engine;
using ConsoleUI.ConsoleCommanding.Services;
using ConsoleUI.ConsoleLogic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ConsoleUI.ConsoleUI
{
  public partial class ConsoleOverlay : Window
  {
    private const int MaxUiEntries = ConsoleTextManager.MaxBufferSize;
    private const int UiTrimChunk = 200;
    private const int MaxPendingEntries = 20000;
    private const int MaxBatchEntries = 200;
    private const int MaxToasts = 3;
    private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(2);

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

    private readonly object _pendingLock = new();
    private readonly Queue<LogEntry> _pendingEntries = new();
    private readonly DispatcherTimer _renderTimer;
    private int _renderScheduled;

    private ObservableCollection<LogEntry> _singleEntries = new();
    private ObservableCollection<LogEntry> _uiEntries = new();
    private ObservableCollection<LogEntry> _devEntries = new();
    private readonly ObservableCollection<ToastNotification> _toasts = new();
    private readonly Dictionary<ToastNotification, CancellationTokenSource> _toastTokens = new();

    private ListBox? _singleList;
    private ListBox? _uiList;
    private ListBox? _devList;

    private ScrollViewer? _singleScroll;
    private ScrollViewer? _uiScroll;
    private ScrollViewer? _devScroll;

    private bool _autoScrollSingle = true;
    private bool _autoScrollUi = true;
    private bool _autoScrollDev = true;

    private sealed class ToastNotification
    {
      public string Text { get; set; } = string.Empty;
    }

    public ConsoleOverlay() : this(false) { }

    public ConsoleOverlay(bool startSplit)
    {
      InitializeComponent();

      _renderTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
      {
        Interval = TimeSpan.FromMilliseconds(33)
      };
      _renderTimer.Tick += (_, _) => FlushPendingEntries();
      ToastHost.ItemsSource = _toasts;

      var writer = new ConsoleWriterAdapter();
      var factory = new CommandFactory();
      var commands = factory.CreateAll(writer);
      _handler = new CommandHandler(commands);
      ConsoleTextManager.Instance.Append("[DEBUG] Handler инициализирован, команд: " + commands.Count);
      _manager = new ConsoleManager(writer, _handler);

      if (startSplit)
      {
        _splitViewEnabled = true;
      }
      InitializeLogView();
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
      lock (_pendingLock)
      {
        _pendingEntries.Enqueue(entry);
        if (_pendingEntries.Count > MaxPendingEntries)
        {
          _pendingEntries.Clear();
          _pendingEntries.Enqueue(new LogEntry
          {
            Text = "[WARN] Консоль перегружена. Показаны только последние сообщения.",
            Color = Brushes.Goldenrod
          });
        }
      }

      if (Interlocked.Exchange(ref _renderScheduled, 1) == 0)
      {
        Dispatcher.BeginInvoke(new Action(() =>
        {
          _renderScheduled = 0;
          if (!_renderTimer.IsEnabled)
            _renderTimer.Start();
        }), DispatcherPriority.Background);
      }
    }

    private void FlushPendingEntries()
    {
      List<LogEntry> batch = new();
      lock (_pendingLock)
      {
        int count = 0;
        while (count < MaxBatchEntries && _pendingEntries.Count > 0)
        {
          batch.Add(_pendingEntries.Dequeue());
          count++;
        }

        if (_pendingEntries.Count == 0)
          _renderTimer.Stop();
      }

      if (batch.Count == 0)
        return;

      if (_splitViewEnabled)
      {
        foreach (var entry in batch)
        {
          if (TryClassify(entry, out bool isDevice))
          {
            if (isDevice)
              _devEntries.Add(entry);
            else
              _uiEntries.Add(entry);
          }
          else
          {
            _uiEntries.Add(entry);
          }
        }

        TrimCollection(_uiEntries);
        TrimCollection(_devEntries);

        if (_autoScrollUi)
          ScrollToEnd(_uiList, _uiEntries);
        if (_autoScrollDev)
          ScrollToEnd(_devList, _devEntries);
        return;
      }

      foreach (var entry in batch)
        _singleEntries.Add(entry);

      TrimCollection(_singleEntries);

      if (_autoScrollSingle)
        ScrollToEnd(_singleList, _singleEntries);
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


    private void InitializeLogView()
    {
      if (_splitViewEnabled)
        BuildSplitView();
      else
        BuildSingleView();
    }

    private void BuildSingleView()
    {
      _singleEntries = new ObservableCollection<LogEntry>();
      _singleList = CreateLogListBox();
      _singleList.ItemsSource = _singleEntries;
      WireAutoScroll(_singleList, value => _autoScrollSingle = value, scroll => _singleScroll = scroll);

      ConsoleHost.Content = _singleList;
    }

    private void BuildSplitView()
    {
      _uiEntries = new ObservableCollection<LogEntry>();
      _devEntries = new ObservableCollection<LogEntry>();

      _uiList = CreateLogListBox();
      _devList = CreateLogListBox();
      _uiList.ItemsSource = _uiEntries;
      _devList.ItemsSource = _devEntries;

      WireAutoScroll(_uiList, value => _autoScrollUi = value, scroll => _uiScroll = scroll);
      WireAutoScroll(_devList, value => _autoScrollDev = value, scroll => _devScroll = scroll);

      var root = new Grid { Margin = new Thickness(0) };
      root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
      root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

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

      Grid.SetRow(_uiList, 1); Grid.SetColumn(_uiList, 0);
      Grid.SetRow(_devList, 1); Grid.SetColumn(_devList, 2);

      root.Children.Add(uiHeader);
      root.Children.Add(divider);
      root.Children.Add(devHeader);
      root.Children.Add(_uiList);
      root.Children.Add(_devList);

      ConsoleHost.Content = root;
    }

    private ListBox CreateLogListBox()
    {
      return new ListBox
      {
        Style = (Style)FindResource("ConsoleListBoxStyle"),
        Focusable = false,
        IsTabStop = false
      };
    }

    private void WireAutoScroll(ListBox listBox, Action<bool> setFlag, Action<ScrollViewer?> setScrollViewer)
    {
      listBox.Loaded += (_, _) =>
      {
        var scroll = FindScrollViewer(listBox);
        setScrollViewer(scroll);
        if (scroll == null) return;

        scroll.ScrollChanged += (_, e) =>
        {
          if (e.ExtentHeightChange == 0)
            setFlag(IsNearBottom(scroll));
        };
      };
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
      if (root is ScrollViewer sv)
        return sv;

      int count = VisualTreeHelper.GetChildrenCount(root);
      for (int i = 0; i < count; i++)
      {
        var child = VisualTreeHelper.GetChild(root, i);
        var result = FindScrollViewer(child);
        if (result != null)
          return result;
      }

      return null;
    }

    private static bool IsNearBottom(ScrollViewer scroll)
    {
      if (scroll.ScrollableHeight <= 0)
        return true;

      return scroll.VerticalOffset >= scroll.ScrollableHeight - 1.0;
    }

    private static void TrimCollection(ObservableCollection<LogEntry> items)
    {
      if (items.Count <= MaxUiEntries + UiTrimChunk)
        return;

      int remove = items.Count - MaxUiEntries;
      for (int i = 0; i < remove; i++)
        items.RemoveAt(0);
    }

    private static void ScrollToEnd(ListBox? listBox, ObservableCollection<LogEntry> items)
    {
      if (listBox == null || items.Count == 0)
        return;

      listBox.ScrollIntoView(items[^1]);
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
      lock (_pendingLock)
        _pendingEntries.Clear();

      if (_splitViewEnabled)
      {
        _uiEntries.Clear();
        _devEntries.Clear();
        return;
      }

      _singleEntries.Clear();
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

    private void LogItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is ListBoxItem item && item.DataContext is LogEntry entry)
      {
        if (!string.IsNullOrEmpty(entry.Text))
          Clipboard.SetText(entry.Text);

        ShowToast("Текст скопирован");
        e.Handled = true;
      }
    }

    private void ShowToast(string message)
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(() => ShowToast(message));
        return;
      }

      if (_toasts.Count >= MaxToasts)
        RemoveToast(_toasts[0]);

      var toast = new ToastNotification { Text = message };
      _toasts.Add(toast);

      var cts = new CancellationTokenSource();
      _toastTokens[toast] = cts;

      _ = RemoveToastAfterDelayAsync(toast, cts.Token);
    }

    private async Task RemoveToastAfterDelayAsync(ToastNotification toast, CancellationToken token)
    {
      try
      {
        await Task.Delay(ToastDuration, token);
      }
      catch (TaskCanceledException)
      {
        return;
      }

      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(() => RemoveToast(toast));
        return;
      }

      RemoveToast(toast);
    }

    private void RemoveToast(ToastNotification toast)
    {
      if (!_toasts.Remove(toast))
        return;

      if (_toastTokens.TryGetValue(toast, out var cts))
      {
        _toastTokens.Remove(toast);
        cts.Cancel();
        cts.Dispose();
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
