using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.UI.Services.Notifications;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
    private ScrollViewer? _protocolScrollViewer;
    private ProtocolCommandGroup? _currentGroup;
    private ProtocolCommandGroup? _pendingGroup;
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
      PreviewKeyDown += ProtocolListBoxUI_PreviewKeyDown;
      Loaded += ProtocolListBoxUI_Loaded;
      Unloaded += ProtocolListBoxUI_Unloaded;
    }

    private void ProtocolListBoxUI_Loaded(object sender, RoutedEventArgs e)
    {
      _protocolScrollViewer ??= FindVisualChild<ScrollViewer>(ProtocolListBox);

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

      if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
      {
        e.Handled = true;

        var text = GetText();
        await PrintOperationNotificationService.PrintTextAsync(text, "Печать протокола");
      }
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
        double delta = e.Delta > 0 ? -MouseWheelScrollStep : MouseWheelScrollStep;
        _protocolScrollViewer.ScrollToVerticalOffset(_protocolScrollViewer.VerticalOffset + delta);
        e.Handled = true;
      }
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
        }
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

      if (_currentGroup != null)
      {
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

    private void RestoreVisibleItems()
    {
      DisplayItems.Clear();
      _currentGroup = null;
      _pendingGroup = null;
      for (int i = 0; i < _historyMessages.Count; i++)
      {
        AppendVisibleMessage(_historyMessages[i]);
      }

      FinalizeLatestCommandGroup();
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
