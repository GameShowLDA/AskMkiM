using Ask.Core.Services.FilesUtility;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private const int MaxVisibleProtocolRows = 3000;

    private readonly List<ShowMessageModel> _historyMessages = new();
    private ScrollViewer? _protocolScrollViewer;
    private ProtocolCommandGroup? _currentGroup;
    private ProtocolCommandGroup? _pendingGroup;
    private bool _scrollToEndRequested;
    private bool _themeSubscribed;
    private int _visibleRowCount;

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

      if (_themeSubscribed)
      {
        return;
      }

      ThemeSettings.ThemeChanged += ProtocolListBoxUI_ThemeChanged;
      _themeSubscribed = true;
    }

    private void ProtocolListBoxUI_Unloaded(object sender, RoutedEventArgs e)
    {
      if (!_themeSubscribed)
      {
        return;
      }

      ThemeSettings.ThemeChanged -= ProtocolListBoxUI_ThemeChanged;
      _themeSubscribed = false;
    }

    private void ProtocolListBoxUI_ThemeChanged(ThemeMode theme)
    {
      Dispatcher.BeginInvoke(
        new Action(RefreshThemeColors),
        DispatcherPriority.Loaded);
    }

    private void ProtocolListBoxUI_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (HandleZoomShortcuts(e))
      {
        return;
      }

      if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
      {
        e.Handled = true;

        try
        {
          var text = GetText();
          TextPrintHelper.PrintText(text, "Печать протокола");
        }
        catch (Exception ex)
        {
          MessageBox.Show(
            $"Ошибка при печати: {ex.Message}",
            "Ошибка печати",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        }
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

        _historyMessages.RemoveRange(_historyMessages.Count - linesToRemove, linesToRemove);
        RestoreVisibleItems();
        removed = linesToRemove;
      });

      return Task.FromResult(removed);
    }

    public async Task ClearAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        _historyMessages.Clear();
        DisplayItems.Clear();
        _currentGroup = null;
        _pendingGroup = null;
        _visibleRowCount = 0;

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
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        _historyMessages.Add(showMessageModel);
        AppendVisibleMessage(showMessageModel);

        if (lastMessage)
        {
          CollapseLatestCommandGroup();
        }

        TrimVisibleItemsIfNeeded();
        RequestScrollToEnd();
      });
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
          _visibleRowCount++;
        }

        return;
      }

      DisplayItems.Add(lineItem);
      _visibleRowCount++;
    }

    private void StartCommandGroup(ShowMessageModel model)
    {
      model.Header = model.Header?.TrimStart() ?? string.Empty;

      CollapseLatestCommandGroup();

      var group = new ProtocolCommandGroup(model);
      _pendingGroup = group;

      DisplayItems.Add(group.HeaderItem);
      _visibleRowCount++;
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

    private void CollapseLatestCommandGroup()
    {
      if (_currentGroup != null)
      {
        CollapseGroup(_currentGroup);
        _currentGroup = null;
      }

      if (_pendingGroup != null)
      {
        _pendingGroup.SetExpanded(false);
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

        _visibleRowCount -= group.VisibleBodyCount;
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
      _visibleRowCount += group.VisibleBodyCount;
      TrimVisibleItemsIfNeeded();
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
      _visibleRowCount = 0;

      int historyStart = Math.Max(0, _historyMessages.Count - MaxVisibleProtocolRows);

      for (int i = historyStart; i < _historyMessages.Count; i++)
      {
        AppendVisibleMessage(_historyMessages[i]);
      }
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

    private void TrimVisibleItemsIfNeeded()
    {
      while (_visibleRowCount > MaxVisibleProtocolRows && DisplayItems.Count > 0)
      {
        RemoveFirstVisibleChunk();
      }
    }

    private void RemoveFirstVisibleChunk()
    {
      if (DisplayItems.Count == 0)
      {
        return;
      }

      var firstItem = DisplayItems[0];

      if (firstItem.IsCommandHeader && firstItem.Group != null)
      {
        var group = firstItem.Group;
        int rowsToRemove = 1 + group.VisibleBodyCount;

        for (int i = 0; i < rowsToRemove; i++)
        {
          DisplayItems.RemoveAt(0);
        }

        if (ReferenceEquals(_currentGroup, group))
        {
          _currentGroup = null;
        }

        if (ReferenceEquals(_pendingGroup, group))
        {
          _pendingGroup = null;
        }

        _visibleRowCount -= rowsToRemove;
        group.VisibleBodyCount = 0;
        return;
      }

      DisplayItems.RemoveAt(0);
      _visibleRowCount--;
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

    public async Task ShowMessageAsync(
      ShowMessageModel model,
      bool IsBlockStart = false,
      bool SkipStepModeCheck = false,
      bool skipPause = false,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0)
    {
      await AppendLineAsync(model);
    }

    public string GetText()
    {
      return string.Join(Environment.NewLine, _historyMessages.Select(m =>
      {
        string indent = new string(' ', m.IndentLevel * 2);
        string header = string.IsNullOrWhiteSpace(m.Header) ? string.Empty : $"{m.Header}: ";
        string timePart = string.IsNullOrWhiteSpace(m.Time) ? string.Empty : $" | {m.Time}";
        return $"{indent}{header}{m.Message}{timePart}";
      }));
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
