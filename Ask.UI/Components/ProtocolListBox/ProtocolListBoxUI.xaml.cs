using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.Protocols;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.UI.Controls.TextEditorControl;
using Ask.UI.Services.Notifications;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Components.ProtocolListBox
{
  /// <summary>
  /// Плоский и виртуализируемый вывод протокола.
  /// Главные команды отображаются отдельными строками-заголовками,
  /// а их содержимое вставляется в общий список только пока команда раскрыта.
  /// </summary>
  public partial class ProtocolListBoxUI : UserControl, IMessageOutputService
  {
    private const double MinFontSize = 12.0;
    private const double MaxFontSize = 48.0;
    private const double ZoomStep = 1.0;
    private const double DefaultFontSize = 20.0;
    private const double MouseWheelScrollStep = 48.0;
    private readonly List<ShowMessageModel> _historyMessages = new();
    private readonly List<(int LineNumber, ErrorOverviewSeverity Severity, string Message)> _errorOverviewDiagnostics = new();
    private readonly List<(int LineNumber, ErrorOverviewSeverity Severity, string Message)> _overviewDiagnostics = new();
    private ScrollViewer? _protocolScrollViewer;
    private bool _protocolScrollViewerSubscribed;
    private ProtocolCommandGroup? _currentGroup;
    private ProtocolCommandGroup? _pendingGroup;
    private int _errorOverviewIndex = -1;
    private ShowMessageModel? _activeErrorMessage;
    private readonly Dictionary<ShowMessageModel, int> _messageIndices = new();
    private bool _overviewUpdatePending;
    private bool _overviewDiagnosticsDirty;
    private bool _visibleIndicesDirty = true;
    private readonly Dictionary<ProtocolDisplayItem, int> _visibleIndices = new();
    private readonly Dictionary<ShowMessageModel, ProtocolDisplayItem> _messageItems = new();
    private readonly Dictionary<ProtocolDisplayItem, ProtocolCommandGroup> _itemGroups = new();
    private ProtocolDisplayItem? _lastMessageItem;
    private bool _scrollToEndRequested;
    private bool _settingsSubscribed;
    private bool _themeSubscribed;
    /// <summary>
    /// Признак запланированного замера задержки отрисовки.
    /// </summary>
    private bool _renderProbePending;

    /// <summary>
    /// Количество записей, ожидающих ближайшего цикла отрисовки.
    /// </summary>
    private int _pendingRenderEntries;

    /// <summary>
    /// Метка времени добавления последней записи протокола.
    /// </summary>
    private long _lastAppendTimestamp;

    /// <summary>
    /// Идентификатор последней записи протокола, ожидающей отрисовки.
    /// </summary>
    private int _lastRenderedMessageId;

    public static readonly DependencyProperty ProtocolFontSizeProperty =
      DependencyProperty.Register(
        nameof(ProtocolFontSize),
        typeof(double),
        typeof(ProtocolListBoxUI),
        new PropertyMetadata(DefaultFontSize));

    /// <summary>
    /// Размер шрифта строк протокола.
    /// </summary>
    public double ProtocolFontSize
    {
      get => (double)GetValue(ProtocolFontSizeProperty);
      set => SetValue(ProtocolFontSizeProperty, value);
    }

    /// <summary>
    /// Видимые строки плоского списка.
    /// </summary>
    public ObservableCollection<ProtocolDisplayItem> DisplayItems { get; } = new();

    public string Header { get; set; } = string.Empty;

    public bool HasRetryAction => throw new NotImplementedException();

    public bool ClickRetry
    {
      get => throw new NotImplementedException();
      set => throw new NotImplementedException();
    }

    public IButtonService ButtonService
    {
      get => throw new NotImplementedException();
      set => throw new NotImplementedException();
    }

    public ProtocolListBoxUI()
    {
      InitializeComponent();
      errorOverviewBar.SetPositionPreviewFactory(GetOverviewPreview);
      DisplayItems.CollectionChanged += (_, _) =>
      {
        _visibleIndicesDirty = true;
        RequestOverviewUpdate(diagnosticsChanged: true);
      };
      PreviewKeyDown += ProtocolListBoxUI_PreviewKeyDown;
      Loaded += ProtocolListBoxUI_Loaded;
      Unloaded += ProtocolListBoxUI_Unloaded;
    }

    private void ProtocolListBoxUI_Loaded(object sender, RoutedEventArgs e)
    {
      _protocolScrollViewer ??= FindVisualChild<ScrollViewer>(ProtocolListBox);
      if (_protocolScrollViewer != null && !_protocolScrollViewerSubscribed)
      {
        _protocolScrollViewer.ScrollChanged += ProtocolScrollViewer_ScrollChanged;
        _protocolScrollViewerSubscribed = true;
      }
      RefreshErrorOverviewViewport();
      RefreshVerticalScrollBar();
      Dispatcher.BeginInvoke(
        () => RefreshOverviewPositions(_protocolScrollViewer?.ExtentHeight ?? 0),
        DispatcherPriority.ContextIdle);

      if (!_themeSubscribed)
      {
        ThemeSettings.ThemeChanged += ProtocolListBoxUI_ThemeChanged;
        _themeSubscribed = true;
      }

      if (!_settingsSubscribed)
      {
        UserInterfaceConfig.SaveUserInterfaceEvent += ProtocolListBoxUI_UserInterfaceSettingsSaved;
        ProtocolConfig.SaveProtocolEvent += ProtocolListBoxUI_ProtocolSettingsSaved;
        _settingsSubscribed = true;
      }
    }

    private void ProtocolListBoxUI_Unloaded(object sender, RoutedEventArgs e)
    {
      if (_protocolScrollViewerSubscribed && _protocolScrollViewer != null)
      {
        _protocolScrollViewer.ScrollChanged -= ProtocolScrollViewer_ScrollChanged;
        _protocolScrollViewerSubscribed = false;
      }

      if (!_themeSubscribed)
      {
        return;
      }

      ThemeSettings.ThemeChanged -= ProtocolListBoxUI_ThemeChanged;
      _themeSubscribed = false;

      if (_settingsSubscribed)
      {
        UserInterfaceConfig.SaveUserInterfaceEvent -= ProtocolListBoxUI_UserInterfaceSettingsSaved;
        ProtocolConfig.SaveProtocolEvent -= ProtocolListBoxUI_ProtocolSettingsSaved;
        _settingsSubscribed = false;
      }
    }

    private void ProtocolListBoxUI_ThemeChanged(ThemeMode theme)
    {
      Dispatcher.BeginInvoke(
        new Action(RefreshThemeColors),
        DispatcherPriority.Loaded);
    }

    private void ProtocolListBoxUI_UserInterfaceSettingsSaved(UserInterfaceDto _)
    {
      Dispatcher.BeginInvoke(
        new Action(RefreshVisibleState),
        DispatcherPriority.Loaded);
    }

    private void ProtocolListBoxUI_ProtocolSettingsSaved(SettingsProtocolDto _)
    {
      Dispatcher.BeginInvoke(
        new Action(RefreshVisibleState),
        DispatcherPriority.Loaded);
    }

    private async void ProtocolListBoxUI_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (HandleZoomShortcuts(e))
      {
        return;
      }

      if (e.Key == Key.F8 &&
          (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift))
      {
        NavigateToOverviewError(Keyboard.Modifiers == ModifierKeys.Shift);
        e.Handled = true;
        return;
      }

      if (e.OriginalSource is TextBox textBox)
      {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.A)
        {
          textBox.SelectAll();
          e.Handled = true;
          return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C && textBox.SelectionLength > 0)
        {
          Clipboard.SetText(textBox.SelectedText);
          e.Handled = true;
          return;
        }
      }

      if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.A)
      {
        ProtocolListBox.SelectAll();
        e.Handled = true;
        return;
      }

      if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
      {
        CopySelectedLines();
        e.Handled = true;
        return;
      }

      if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
      {
        e.Handled = true;

        var text = GetText();
        await PrintOperationNotificationService.PrintTextAsync(text, "Печать протокола");
      }
    }

    private void CopySelectedLines()
    {
      var selected = ProtocolListBox.SelectedItems
        .OfType<ProtocolDisplayItem>()
        .ToHashSet();
      if (selected.Count == 0)
        return;

      string text = string.Join(
        Environment.NewLine,
        DisplayItems
          .Where(selected.Contains)
          .Select(item => FormatDisplayItemForClipboard(item.Message)));

      if (!string.IsNullOrEmpty(text))
        Clipboard.SetText(text);
    }

    private static string FormatDisplayItemForClipboard(ShowMessageModel message)
    {
      string line = ExecutionProtocolLineFormatter.Format(message);
      if (string.IsNullOrEmpty(message.Debug))
        return line;

      return string.IsNullOrEmpty(line)
        ? message.Debug.TrimStart('\r', '\n')
        : line + message.Debug;
    }

    private void ProtocolListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
      {
        if (e.Delta > 0)
        {
          Zoom(true);
        }
        else if (e.Delta < 0)
        {
          Zoom(false);
        }

        e.Handled = true;
        return;
      }

      if (_protocolScrollViewer != null)
      {
        double delta = -e.Delta / 120.0 * (SystemParameters.WheelScrollLines < 0
          ? _protocolScrollViewer.ViewportHeight
          : SystemParameters.WheelScrollLines * MouseWheelScrollStep / 3.0);
        _protocolScrollViewer.ScrollToVerticalOffset(_protocolScrollViewer.VerticalOffset + delta);
        e.Handled = true;
      }
    }

    private void ProtocolScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      RefreshVerticalScrollBar();
      RequestOverviewUpdate(diagnosticsChanged: false);
    }

    private void RefreshVerticalScrollBar()
    {
      if (_protocolScrollViewer == null) return;
      ProtocolVerticalScrollBar.Maximum = _protocolScrollViewer.ScrollableHeight;
      ProtocolVerticalScrollBar.ViewportSize = _protocolScrollViewer.ViewportHeight;
      ProtocolVerticalScrollBar.LargeChange = _protocolScrollViewer.ViewportHeight;
      ProtocolVerticalScrollBar.SmallChange = MouseWheelScrollStep;
      ProtocolVerticalScrollBar.Value = _protocolScrollViewer.VerticalOffset;
      ProtocolVerticalScrollBar.Visibility = _protocolScrollViewer.ScrollableHeight > 0
        ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ProtocolVerticalScrollBar_Scroll(object sender, ScrollEventArgs e)
    {
      _protocolScrollViewer?.ScrollToVerticalOffset(e.NewValue);
    }

    private void RefreshErrorOverviewViewport()
    {
      if (_protocolScrollViewer == null || _protocolScrollViewer.ExtentHeight <= 0)
      {
        errorOverviewBar.SetViewport(0, 1);
        return;
      }

      double extent = _protocolScrollViewer.ExtentHeight;
      errorOverviewBar.SetViewport(_protocolScrollViewer.VerticalOffset / extent,
        (_protocolScrollViewer.VerticalOffset + _protocolScrollViewer.ViewportHeight) / extent);

    }

    private void RefreshOverviewPositions(double extent)
    {
      if (_visibleIndicesDirty)
      {
        _visibleIndices.Clear();
        for (int i = 0; i < DisplayItems.Count; i++) _visibleIndices[DisplayItems[i]] = i;
        _visibleIndicesDirty = false;
      }

      var samples = new SortedDictionary<int, double> { [0] = 0, [DisplayItems.Count] = extent };
      var panel = FindVisualChild<VirtualizingStackPanel>(ProtocolListBox);
      var presenter = FindVisualChild<ScrollContentPresenter>(_protocolScrollViewer!);
      if (panel != null && presenter != null)
      {
        foreach (var container in panel.Children.OfType<ListBoxItem>())
        {
          if (container.DataContext is not ProtocolDisplayItem item ||
              !_visibleIndices.TryGetValue(item, out int index)) continue;
          double offset = container.TranslatePoint(new Point(), presenter).Y + _protocolScrollViewer!.VerticalOffset;
          if (index > 0) samples[index] = Math.Clamp(offset, 0, extent);
          if (index + 1 < DisplayItems.Count)
            samples[index + 1] = Math.Clamp(offset + container.ActualHeight, 0, extent);
        }
      }
      var anchors = samples.Select(pair => (Index: pair.Key, Offset: pair.Value)).ToArray();
      var positions = new Dictionary<int, double>();
      foreach (var diagnostic in _overviewDiagnostics)
      {
        var message = _historyMessages[diagnostic.LineNumber - 1];
        if (!_messageItems.TryGetValue(message, out var item)) continue;
        if (_itemGroups.TryGetValue(item, out var group) && !group.IsExpanded) item = group.HeaderItem;
        if (_visibleIndices.TryGetValue(item, out int index))
          positions[diagnostic.LineNumber] = ProjectOverviewOffset(index, anchors) / extent;
      }
      errorOverviewBar.SetLinePositions(positions, fraction =>
        _protocolScrollViewer!.ScrollToVerticalOffset(Math.Max(0,
          fraction * _protocolScrollViewer.ExtentHeight - _protocolScrollViewer.ViewportHeight / 2)));
    }

    // Нереализованные строки виртуализированного списка оцениваются между измеренными границами.
    internal static double ProjectOverviewOffset(int index, IReadOnlyList<(int Index, double Offset)> anchors)
    {
      int low = 0;
      int high = anchors.Count - 1;
      while (high - low > 1)
      {
        int middle = (low + high) / 2;
        if (anchors[middle].Index <= index) low = middle;
        else high = middle;
      }
      var start = anchors[low];
      var end = anchors[high];
      if (end.Index == start.Index) return start.Offset;
      double fraction = Math.Clamp((double)(index - start.Index) / (end.Index - start.Index), 0, 1);
      return start.Offset + fraction * (end.Offset - start.Offset);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
      if (parent == null)
      {
        return null;
      }

      int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

      for (int i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);

        if (child is T typedChild)
        {
          return typedChild;
        }

        var result = FindVisualChild<T>(child);
        if (result != null)
        {
          return result;
        }
      }

      return null;
    }

    public IReadOnlyList<ShowMessageModel> GetMessagesSnapshot()
    {
      if (Application.Current.Dispatcher.CheckAccess())
      {
        return _historyMessages.ToList();
      }

      return Application.Current.Dispatcher.Invoke(
        () => (IReadOnlyList<ShowMessageModel>)_historyMessages.ToList());
    }

    /// <summary>
    /// Загружает сохранённые сообщения в представление протокола.
    /// </summary>
    public void LoadMessages(IEnumerable<ShowMessageModel> messages)
    {
      ArgumentNullException.ThrowIfNull(messages);

      _historyMessages.Clear();
      bool useSyntaxHighlighting = UserInterfaceConfig.GetSyntaxHighlighting();
      bool useCommandBackgroundHighlighting = UserInterfaceConfig.GetCommandBodyBackgroundHighlighting();
      bool useChainPointBackgroundHighlighting = UserInterfaceConfig.GetChainPointBodyBackgroundHighlighting();

      foreach (var message in messages)
      {
        ApplyThemeColors(
          message,
          useSyntaxHighlighting,
          useCommandBackgroundHighlighting,
          useChainPointBackgroundHighlighting);
        _historyMessages.Add(message);
      }

      RestoreVisibleItems();
    }

    private bool HandleZoomShortcuts(KeyEventArgs e)
    {
      if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
      {
        return false;
      }

      switch (e.Key)
      {
        case Key.OemPlus:
        case Key.Add:
          Zoom(true);
          e.Handled = true;
          return true;

        case Key.OemMinus:
        case Key.Subtract:
          Zoom(false);
          e.Handled = true;
          return true;

        case Key.D0:
        case Key.NumPad0:
          ResetZoom();
          e.Handled = true;
          return true;
      }

      return false;
    }

    private void Zoom(bool zoomIn)
    {
      var candidate = zoomIn
        ? ProtocolFontSize + ZoomStep
        : ProtocolFontSize - ZoomStep;

      SetProtocolFontSize(Clamp(candidate, MinFontSize, MaxFontSize));
    }

    private void ResetZoom()
    {
      SetProtocolFontSize(DefaultFontSize);
    }

    private void SetProtocolFontSize(double size)
    {
      ProtocolFontSize = size;
    }

    private static double Clamp(double value, double min, double max)
      => Math.Max(min, Math.Min(max, value));

    public Task<int> RemoveLastLinesAsync(int count = 1)
    {
      int removed = 0;

      Application.Current.Dispatcher.Invoke(() =>
      {
        int linesToRemove = Math.Min(count, _historyMessages.Count);
        if (linesToRemove <= 0)
        {
          return;
        }

        int removeStartIndex = _historyMessages.Count - linesToRemove;
        var removedMessages = _historyMessages.GetRange(removeStartIndex, linesToRemove);
        _historyMessages.RemoveRange(removeStartIndex, linesToRemove);

        for (int i = removedMessages.Count - 1; i >= 0; i--)
        {
          RemoveLastVisibleMessage(removedMessages[i]);
          if (_messageItems.TryGetValue(removedMessages[i], out var removedItem) &&
              ReferenceEquals(removedItem.Message, removedMessages[i])) _itemGroups.Remove(removedItem);
          _messageItems.Remove(removedMessages[i]);
        }
        _lastMessageItem = _historyMessages.Count > 0 &&
          _messageItems.TryGetValue(_historyMessages[^1], out var tailItem) ? tailItem : null;
        RefreshErrorOverview();
        removed = linesToRemove;
      });

      return Task.FromResult(removed);
    }

    private void RemoveLastVisibleMessage(ShowMessageModel removedMessage)
    {
      if (_pendingGroup != null && ReferenceEquals(_pendingGroup.HeaderItem.Message, removedMessage))
      {
        RemoveVisibleTailItem(_pendingGroup.HeaderItem);
        _pendingGroup = null;
        return;
      }

      if (_currentGroup != null && _currentGroup.BodyItems.Count > 0)
      {
        var lastBodyItem = _currentGroup.BodyItems[^1];
        if (ReferenceEquals(lastBodyItem.Message, removedMessage))
        {
          if (RemoveVisibleTailItem(lastBodyItem))
          {
            _currentGroup.VisibleBodyCount--;
          }

          _currentGroup.RemoveLastBodyItem(removedMessage);
          return;
        }
      }

      var lastDisplayItem = DisplayItems.LastOrDefault();
      if (lastDisplayItem != null && ReferenceEquals(lastDisplayItem.Message, removedMessage))
      {
        RemoveVisibleTailItem(lastDisplayItem);
      }
    }

    private bool RemoveVisibleTailItem(ProtocolDisplayItem item)
    {
      if (DisplayItems.Count == 0 || !ReferenceEquals(DisplayItems[^1], item))
      {
        return false;
      }

      DisplayItems.RemoveAt(DisplayItems.Count - 1);
      return true;
    }

    public async Task ClearAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        _historyMessages.Clear();
        DisplayItems.Clear();
        _currentGroup = null;
        _pendingGroup = null;
        _lastMessageItem = null;
        _activeErrorMessage = null;
        _messageItems.Clear();
        _itemGroups.Clear();
        RefreshErrorOverview();
        LogInformation("Протокол полностью очищен.");
      });
    }

    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove)
    {
      return await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        try
        {
          var target = _historyMessages.FirstOrDefault(m =>
            (!string.IsNullOrEmpty(m.Header) && m.Header.Contains(textToRemove, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(m.Message) && m.Message.Contains(textToRemove, StringComparison.OrdinalIgnoreCase)));

          if (target == null)
          {
            LogWarning($"Строка '{textToRemove}' не найдена.");
            return false;
          }

          _historyMessages.Remove(target);
          RestoreVisibleItems();
          LogInformation($"Строка '{textToRemove}' найдена и удалена.");
          return true;
        }
        catch (Exception ex)
        {
          LogException("Ошибка при удалении строки", ex);
          return false;
        }
      });
    }

    public async Task AppendLineAsync(ShowMessageModel showMessageModel, bool lastMessage = false)
    {
      var queuedAt = Stopwatch.GetTimestamp();
      var messageId = RuntimeHelpers.GetHashCode(showMessageModel);
      var dispatcherQueueMs = 0d;
      var uiWorkMs = 0d;

      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        var uiWorkStarted = Stopwatch.GetTimestamp();
        dispatcherQueueMs = Stopwatch.GetElapsedTime(queuedAt, uiWorkStarted).TotalMilliseconds;
        var shouldScrollToEnd = IsScrolledToEnd();

        _historyMessages.Add(showMessageModel);
        AppendVisibleMessage(showMessageModel);
        _messageIndices[showMessageModel] = _historyMessages.Count - 1;
        AddOverviewDiagnostic(showMessageModel, _historyMessages.Count);
        RequestOverviewUpdate();

        if (lastMessage)
        {
          FinalizeLatestCommandGroup();
        }

        if (shouldScrollToEnd)
        {
          RequestScrollToEnd();
        }
        RequestRenderTimingProbe(messageId);

        uiWorkMs = Stopwatch.GetElapsedTime(uiWorkStarted).TotalMilliseconds;
      }, DispatcherPriority.Background);

      LogDebug(
        $"[ProtocolOutputTiming] UI append completed: message={messageId}, " +
        $"dispatcherQueueMs={dispatcherQueueMs:F1}, uiWorkMs={uiWorkMs:F1}, " +
        $"totalMs={Stopwatch.GetElapsedTime(queuedAt).TotalMilliseconds:F1}, " +
        $"thread={Environment.CurrentManagedThreadId}");
    }

    /// <summary>
    /// Регистрирует задержку до ближайшего цикла отрисовки протокола.
    /// </summary>
    /// <param name="messageId">Идентификатор записи протокола.</param>
    private void RequestRenderTimingProbe(int messageId)
    {
      _pendingRenderEntries++;
      _lastAppendTimestamp = Stopwatch.GetTimestamp();
      _lastRenderedMessageId = messageId;

      if (_renderProbePending)
      {
        return;
      }

      _renderProbePending = true;
      Dispatcher.BeginInvoke(() =>
      {
        var entries = _pendingRenderEntries;
        var lastMessageId = _lastRenderedMessageId;
        var renderLatencyMs = Stopwatch.GetElapsedTime(_lastAppendTimestamp).TotalMilliseconds;

        _pendingRenderEntries = 0;
        _renderProbePending = false;

        LogDebug(
          $"[ProtocolOutputTiming] UI render turn reached: message={lastMessageId}, " +
          $"batchedEntries={entries}, renderLatencyMs={renderLatencyMs:F1}, " +
          $"thread={Environment.CurrentManagedThreadId}");
      }, DispatcherPriority.Render);
    }

    private void AppendVisibleMessage(ShowMessageModel model)
    {
      if (IsStandaloneServiceLog(model) && _lastMessageItem != null)
      {
        _lastMessageItem.AddServiceLog(model.Debug!);
        _messageItems[model] = _lastMessageItem;
        return;
      }

      if (model.Status == ShowMessageModel.MessageType.Command)
      {
        StartCommandGroup(model);
        return;
      }

      AppendLineItem(model);
    }

    private void AppendLineItem(ShowMessageModel model)
    {
      EnsureCurrentGroupStarted();

      var lineItem = ProtocolDisplayItem.CreateLine(model, isInsideCommandGroup: _currentGroup != null);
      _lastMessageItem = lineItem;
      _messageItems[model] = lineItem;

      if (_currentGroup != null)
      {
        _itemGroups[lineItem] = _currentGroup;
        _currentGroup.AddBodyItem(lineItem);

        if (_currentGroup.IsExpanded)
        {
          DisplayItems.Add(lineItem);
          _currentGroup.VisibleBodyCount++;
        }

        return;
      }

      DisplayItems.Add(lineItem);
    }

    private void StartCommandGroup(ShowMessageModel model)
    {
      model.Header = model.Header?.TrimStart() ?? string.Empty;

      FinalizeLatestCommandGroup();

      if (!ProtocolConfig.GetCommandHeadersInProtocol())
      {
        return;
      }

      var group = new ProtocolCommandGroup(model);
      _lastMessageItem = group.HeaderItem;
      _messageItems[model] = group.HeaderItem;
      _pendingGroup = group;

      DisplayItems.Add(group.HeaderItem);
    }

    private void EnsureCurrentGroupStarted()
    {
      if (_pendingGroup == null)
      {
        return;
      }

      _currentGroup = _pendingGroup;
      _pendingGroup = null;
    }

    private void FinalizeLatestCommandGroup()
    {
      bool useCommandAutoCollapse =
        ProtocolConfig.GetCommandHeadersInProtocol() &&
        UserInterfaceConfig.GetCommandAutoCollapse();

      if (_currentGroup != null)
      {
        if (useCommandAutoCollapse)
        {
          CollapseGroup(_currentGroup);
        }

        _currentGroup = null;
      }

      if (_pendingGroup != null)
      {
        if (useCommandAutoCollapse)
        {
          _pendingGroup.SetExpanded(false);
        }

        _pendingGroup = null;
      }
    }

    private void CollapseGroup(ProtocolCommandGroup group)
    {
      if (!group.IsExpanded)
      {
        return;
      }

      if (group.VisibleBodyCount > 0)
      {
        int startIndex = DisplayItems.IndexOf(group.HeaderItem) + 1;

        for (int i = 0; i < group.VisibleBodyCount; i++)
        {
          DisplayItems.RemoveAt(startIndex);
        }

        group.VisibleBodyCount = 0;
      }

      group.SetExpanded(false);
    }

    private void ExpandGroup(ProtocolCommandGroup group)
    {
      if (group.IsExpanded)
      {
        return;
      }

      group.SetExpanded(true);

      if (group.BodyItems.Count == 0)
      {
        return;
      }

      int headerIndex = DisplayItems.IndexOf(group.HeaderItem);
      if (headerIndex < 0)
      {
        return;
      }

      for (int i = 0; i < group.BodyItems.Count; i++)
      {
        DisplayItems.Insert(headerIndex + i + 1, group.BodyItems[i]);
      }

      group.VisibleBodyCount = group.BodyItems.Count;
    }

    private void ProtocolCommandHeaderToggleButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is not ToggleButton { DataContext: ProtocolDisplayItem { IsCommandHeader: true, Group: not null } item })
      {
        return;
      }

      if (item.IsExpanded)
      {
        CollapseGroup(item.Group);
      }
      else
      {
        ExpandGroup(item.Group);
      }

      e.Handled = true;
    }

    private void ServiceLogsToggleButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is ToggleButton { DataContext: ProtocolDisplayItem item })
        item.AreServiceLogsExpanded = !item.AreServiceLogsExpanded;

      e.Handled = true;
    }

    private static bool IsStandaloneServiceLog(ShowMessageModel message)
      => string.IsNullOrWhiteSpace(message.Header) &&
         string.IsNullOrWhiteSpace(message.Message) &&
         message.Debug?.TrimStart().StartsWith("[ЛОГ ROOT]", StringComparison.Ordinal) == true;

    private void RestoreVisibleItems()
    {
      DisplayItems.Clear();
      _currentGroup = null;
      _pendingGroup = null;
      _lastMessageItem = null;
      _messageItems.Clear();
      _itemGroups.Clear();
      for (int i = 0; i < _historyMessages.Count; i++)
      {
        AppendVisibleMessage(_historyMessages[i]);
      }

      FinalizeLatestCommandGroup();
      RefreshErrorOverview();
      RefreshErrorOverviewViewport();
    }

    private void RefreshErrorOverview()
    {
      _errorOverviewDiagnostics.Clear();
      _overviewDiagnostics.Clear();
      _messageIndices.Clear();
      for (int i = 0; i < _historyMessages.Count; i++)
      {
        _messageIndices[_historyMessages[i]] = i;
        AddOverviewDiagnostic(_historyMessages[i], i + 1);
      }
      RequestOverviewUpdate();
    }

    private void AddOverviewDiagnostic(ShowMessageModel message, int lineNumber)
    {
      var severity = GetOverviewSeverity(message);
      if (severity == null) return;
      var diagnostic = (lineNumber, severity.Value, ExecutionProtocolLineFormatter.Format(message));
      _overviewDiagnostics.Add(diagnostic);
      if (severity == ErrorOverviewSeverity.Error) _errorOverviewDiagnostics.Add(diagnostic);
    }

    internal static ErrorOverviewSeverity? GetOverviewSeverity(ShowMessageModel message)
      => IsOverviewError(message) ? ErrorOverviewSeverity.Error
        : message.Status == ShowMessageModel.MessageType.Command
          ? ErrorOverviewSeverity.Information : null;

    internal static bool IsOverviewError(ShowMessageModel message)
      => message.Status == ShowMessageModel.MessageType.Error || message.ExecutionError ||
        ((message.Status == null || message.Status == ShowMessageModel.MessageType.Info) &&
         (message.Header?.Contains("[БРАК]", StringComparison.OrdinalIgnoreCase) == true ||
          message.Message?.Contains("[БРАК]", StringComparison.OrdinalIgnoreCase) == true));

    private void RequestOverviewUpdate(bool diagnosticsChanged = true)
    {
      _overviewDiagnosticsDirty |= diagnosticsChanged;
      if (_overviewUpdatePending) return;
      _overviewUpdatePending = true;
      Dispatcher.BeginInvoke(() =>
      {
        _overviewUpdatePending = false;
        if (_overviewDiagnosticsDirty)
        {
          _overviewDiagnosticsDirty = false;
          errorOverviewBar.SetLineDiagnostics(_historyMessages.Count,
            _overviewDiagnostics, NavigateToErrorOverviewLine);
          RefreshOverviewPositions(_protocolScrollViewer?.ExtentHeight ?? 0);
        }
        UpdateOverviewSelection();
        RefreshErrorOverviewViewport();
      }, DispatcherPriority.Loaded);
    }

    private void UpdateOverviewSelection()
    {
      int line = _activeErrorMessage != null && _messageIndices.TryGetValue(_activeErrorMessage, out int index)
        ? index + 1 : -1;
      _errorOverviewIndex = _errorOverviewDiagnostics.FindIndex(d => d.LineNumber == line);
      errorOverviewBar.SetActiveLine(line);
    }

    private void PreviousErrorButton_Click(object sender, RoutedEventArgs e) => NavigateToOverviewError(true);
    private void NextErrorButton_Click(object sender, RoutedEventArgs e) => NavigateToOverviewError(false);

    internal string? GetOverviewPreview(double fraction)
    {
      if (_historyMessages.Count == 0) return null;

      var relevant = Enumerable.Range(0, _historyMessages.Count)
        .Where(index => GetOverviewSeverity(_historyMessages[index]) != null)
        .ToArray();
      if (relevant.Length == 0) return null;

      int target = (int)Math.Round(Math.Clamp(fraction, 0, 1) * (_historyMessages.Count - 1));
      int centerPosition = Array.BinarySearch(relevant, target);
      if (centerPosition < 0) centerPosition = ~centerPosition;
      centerPosition = Math.Clamp(centerPosition, 0, relevant.Length - 1);
      if (centerPosition > 0 &&
          (centerPosition == relevant.Length - 1 ||
           target - relevant[centerPosition - 1] <= relevant[centerPosition] - target))
      {
        centerPosition--;
      }

      int startPosition = Math.Max(0, centerPosition - 1);
      int endPosition = Math.Min(relevant.Length - 1, centerPosition + 1);
      int centerLine = relevant[centerPosition] + 1;
      var lines = new List<string> { $"Команды и ошибки · строка {centerLine}" };
      for (int position = startPosition; position <= endPosition; position++)
      {
        int index = relevant[position];
        string line = ExecutionProtocolLineFormatter.Format(_historyMessages[index]);
        line = line.Replace("\r", " ").Replace("\n", " ").Trim();
        if (line.Length > 180) line = line[..177] + "...";
        lines.Add($"{(position == centerPosition ? "▸" : " ")} {index + 1,5}: {line}");
      }
      return string.Join(Environment.NewLine, lines);
    }

    internal void NavigateToErrorOverviewLine(int lineNumber)
    {
      int messageIndex = lineNumber - 1;
      if (messageIndex < 0 || messageIndex >= _historyMessages.Count)
      {
        return;
      }

      var targetMessage = _historyMessages[messageIndex];
      _activeErrorMessage = targetMessage;
      _errorOverviewIndex = _errorOverviewDiagnostics.FindIndex(diagnostic =>
        diagnostic.LineNumber == lineNumber);
      _messageItems.TryGetValue(targetMessage, out var item);
      if (item == null)
        item = DisplayItems.FirstOrDefault(displayItem =>
          _messageIndices.TryGetValue(displayItem.Message, out int index) && index >= messageIndex)
          ?? DisplayItems.LastOrDefault();
      if (item?.Group != null) ExpandGroup(item.Group);
      if (item != null && _itemGroups.TryGetValue(item, out var ownerGroup)) ExpandGroup(ownerGroup);
      if (IsStandaloneServiceLog(targetMessage) && item != null) item.AreServiceLogsExpanded = true;

      if (item == null)
      {
        return;
      }

      ProtocolListBox.SelectedItem = item;
      ProtocolListBox.ScrollIntoView(item);
      ProtocolListBox.Focus();
      UpdateOverviewSelection();
    }

    private void NavigateToOverviewError(bool previous)
    {
      if (_errorOverviewDiagnostics.Count == 0)
      {
        return;
      }

      int nextIndex;
      if (_errorOverviewIndex < 0)
      {
        nextIndex = previous ? _errorOverviewDiagnostics.Count - 1 : 0;
      }
      else
      {
        int direction = previous ? -1 : 1;
        nextIndex = (_errorOverviewIndex + direction + _errorOverviewDiagnostics.Count) %
          _errorOverviewDiagnostics.Count;
      }

      NavigateToErrorOverviewLine(_errorOverviewDiagnostics[nextIndex].LineNumber);
    }

    private void RefreshThemeColors()
    {
      bool useSyntaxHighlighting = UserInterfaceConfig.GetSyntaxHighlighting();
      bool useCommandBackgroundHighlighting = UserInterfaceConfig.GetCommandBodyBackgroundHighlighting();
      bool useChainPointBackgroundHighlighting = UserInterfaceConfig.GetChainPointBodyBackgroundHighlighting();

      foreach (var message in _historyMessages)
      {
        ApplyThemeColors(
          message,
          useSyntaxHighlighting,
          useCommandBackgroundHighlighting,
          useChainPointBackgroundHighlighting);
      }

      RestoreVisibleItems();
    }

    private void RefreshVisibleState()
    {
      RestoreVisibleItems();
      RequestScrollToEnd();
    }

    private static void ApplyThemeColors(
      ShowMessageModel message,
      bool useSyntaxHighlighting,
      bool useCommandBackgroundHighlighting,
      bool useChainPointBackgroundHighlighting)
    {
      if (message.HeaderColor == Colors.Transparent && message.MessageColor == Colors.Transparent)
      {
        message.HeaderBackgroundColor = null;
        return;
      }

      bool hadBackground = message.HeaderBackgroundColor.HasValue;
      Color headerForeground = GetThemeColor("TestsProtocolHeaderForeground", Colors.Black);
      Color messageForeground = GetThemeColor("TestsProtocolMessageForeground", headerForeground);
      Color timeForeground = GetThemeColor("TestsProtocolTimeForeground", headerForeground);

      message.HeaderColor = headerForeground;
      message.MessageColor = messageForeground;
      message.TimeColor = timeForeground;
      message.HeaderBackgroundColor = null;

      if (!useSyntaxHighlighting)
      {
        message.MessageColor = headerForeground;
        message.TimeColor = headerForeground;
        return;
      }

      if (message.UseSuccessColorForEntireMessage)
      {
        Color successColor = GetThemeColor("TestsProtocolMessageSuccesForeground", Colors.Green);
        message.HeaderColor = successColor;
        message.MessageColor = successColor;
        message.TimeColor = successColor;
        return;
      }

      switch (message.Status)
      {
        case ShowMessageModel.MessageType.Success:
        case ShowMessageModel.MessageType.Error:
          message.MessageColor = message.GetColorMessage();
          break;

        case ShowMessageModel.MessageType.Command:
          var commandColor = message.GetColorMessage();
          if (commandColor.HasValue)
          {
            message.HeaderColor = commandColor.Value;
            message.MessageColor = commandColor.Value;
            message.HeaderBackgroundColor = useCommandBackgroundHighlighting
              ? BuildPaleTextBackground(commandColor.Value)
              : null;
          }

          break;

        case ShowMessageModel.MessageType.CommandBlock:
          var commandBlockColor = message.GetColorMessage();
          if (commandBlockColor.HasValue)
          {
            message.MessageColor = commandBlockColor.Value;
            message.HeaderBackgroundColor = hadBackground && useChainPointBackgroundHighlighting
              ? BuildPaleTextBackground(commandBlockColor.Value)
              : null;
          }

          break;
      }
    }

    private static Color BuildPaleTextBackground(Color textColor)
    {
      const byte paleAlpha = 70;
      return Color.FromArgb(paleAlpha, textColor.R, textColor.G, textColor.B);
    }

    private static Color GetThemeColor(string resourceKey, Color fallbackColor)
    {
      if (Application.Current?.Resources[resourceKey] is SolidColorBrush brush)
      {
        return brush.Color;
      }

      return fallbackColor;
    }

    private void RequestScrollToEnd()
    {
      if (_scrollToEndRequested)
      {
        return;
      }

      _scrollToEndRequested = true;

      void HandleLayoutUpdated(object? sender, EventArgs e)
      {
        ProtocolListBox.LayoutUpdated -= HandleLayoutUpdated;
        _scrollToEndRequested = false;

        _protocolScrollViewer ??= FindVisualChild<ScrollViewer>(ProtocolListBox);
        if (_protocolScrollViewer == null)
        {
          return;
        }

        _protocolScrollViewer.ScrollToVerticalOffset(_protocolScrollViewer.ExtentHeight);
      }

      ProtocolListBox.LayoutUpdated += HandleLayoutUpdated;

      Dispatcher.BeginInvoke(() =>
      {
        ProtocolListBox.InvalidateMeasure();
        ProtocolListBox.InvalidateArrange();
      }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Проверяет, находится ли область просмотра у последней строки протокола.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если включена автоматическая прокрутка к новым строкам.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    private bool IsScrolledToEnd()
    {
      _protocolScrollViewer ??= FindVisualChild<ScrollViewer>(ProtocolListBox);
      if (_protocolScrollViewer == null)
      {
        return true;
      }

      const double tolerance = 2.0;
      return _protocolScrollViewer.VerticalOffset >=
        _protocolScrollViewer.ScrollableHeight - tolerance;
    }

    public Task AppendEmptyLineAsync(int indentLevel = 0)
    {
      var emptyLine = new ShowMessageModel
      {
        Header = string.Empty,
        Message = string.Empty,
        Time = string.Empty,
        HeaderColor = Colors.Transparent,
        MessageColor = Colors.Transparent,
        IndentLevel = indentLevel
      };

      return AppendLineAsync(emptyLine);
    }

    public Task CompleteCommandAsync(bool hasErrors)
    {
      return Application.Current.Dispatcher.InvokeAsync(() =>
      {
        var activeGroup = _currentGroup ?? _pendingGroup;
        activeGroup?.SetExecutionResult(hasErrors);
      }).Task;
    }

    /// <summary>
    /// Завершает текущую группу команды, чтобы последующие сообщения отображались вне неё.
    /// </summary>
    public Task FinalizeCurrentCommandGroupAsync()
    {
      return Application.Current.Dispatcher.InvokeAsync(FinalizeLatestCommandGroup).Task;
    }

    public async Task ShowMessageAsync(
      ShowMessageModel model,
      bool IsBlockStart = false,
      bool SkipStepModeCheck = false,
      bool skipPause = false,
      bool ignoreOutputValidation = false,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0)
    {
      await AppendLineAsync(model);
    }

    public string GetText()
    {
      return string.Join(Environment.NewLine, _historyMessages
        .Where(message => ProtocolConfig.GetCommandHeadersInProtocol() || message.Status != ShowMessageModel.MessageType.Command)
        .Select(Ask.Core.Services.Protocols.ExecutionProtocolLineFormatter.Format));
    }

    public int GetLastLineNumber()
    {
      int count = _historyMessages.Count;
      return count > 0 ? count - 1 : -1;
    }

    public async Task MoveToLineAsync(int lineNumber)
    {
      if (DisplayItems.Count == 0)
      {
        return;
      }

      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        int index = Math.Max(0, Math.Min(lineNumber, DisplayItems.Count - 1));
        var item = DisplayItems[index];
        ProtocolListBox.SelectedItem = item;
        ProtocolListBox.ScrollIntoView(item);
      });
    }
  }
}
