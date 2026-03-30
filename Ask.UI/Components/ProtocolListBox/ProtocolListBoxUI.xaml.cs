using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Components.ProtocolListBox
{
  /// <summary>
  /// Логика взаимодействия для ProtocolListBoxUI.xaml
  /// </summary>
  public partial class ProtocolListBoxUI : UserControl, IMessageOutputService
  {
    private const double MinFontSize = 12.0;
    private const double MaxFontSize = 48.0;
    private const double ZoomStep = 1.0;
    private const double DefaultFontSize = 20.0;
    private const double MouseWheelScrollStep = 48.0;
    private ScrollViewer? _protocolScrollViewer;
    private ProtocolDisplayItem? _currentGroup;
    private ProtocolDisplayItem? _pendingCommandHeaderItem;

    /// <summary>
    /// Размер шрифта строк протокола.
    /// </summary>
    public static readonly DependencyProperty ProtocolFontSizeProperty =
      DependencyProperty.Register(
        nameof(ProtocolFontSize),
        typeof(double),
        typeof(ProtocolListBoxUI),
        new PropertyMetadata(DefaultFontSize));

    /// <summary>
    /// Размер шрифта протокола.
    /// </summary>
    public double ProtocolFontSize
    {
      get => (double)GetValue(ProtocolFontSizeProperty);
      set => SetValue(ProtocolFontSizeProperty, value);
    }

    /// <summary>
    /// Плоский список сообщений протокола.
    /// Старый источник данных, который можно пока оставить без изменений.
    /// </summary>
    public ObservableCollection<ShowMessageModel> Messages { get; } = new();

    /// <summary>
    /// Новая коллекция элементов отображения.
    /// Нужна для будущего сворачиваемого отображения блоков.
    /// </summary>
    public ObservableCollection<ProtocolDisplayItem> DisplayItems { get; } = new();

    public string Header { get; set; }

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
    }

    private void ProtocolListBoxUI_Loaded(object sender, RoutedEventArgs e)
    {
      _protocolScrollViewer ??= FindVisualChild<ScrollViewer>(ProtocolListBox);
    }

    /// <summary>
    /// Обработка клавиатурных сокращений контрола.
    /// </summary>
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

    /// <summary>
    /// Масштабирование текста протокола по Ctrl + колесо мыши.
    /// </summary>
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

    /// <summary>
    /// Обрабатывает Ctrl + '+', Ctrl + '-', Ctrl + '0' для масштаба.
    /// </summary>
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

    /// <summary>
    /// Увеличивает или уменьшает размер шрифта.
    /// </summary>
    private void Zoom(bool zoomIn)
    {
      var candidate = zoomIn
        ? ProtocolFontSize + ZoomStep
        : ProtocolFontSize - ZoomStep;

      SetProtocolFontSize(Clamp(candidate, MinFontSize, MaxFontSize));
    }

    /// <summary>
    /// Сбрасывает масштаб шрифта к значению по умолчанию.
    /// </summary>
    private void ResetZoom()
    {
      SetProtocolFontSize(DefaultFontSize);
    }

    /// <summary>
    /// Устанавливает размер шрифта протокола.
    /// </summary>
    private void SetProtocolFontSize(double size)
    {
      ProtocolFontSize = size;
    }

    /// <summary>
    /// Ограничивает значение диапазоном.
    /// </summary>
    private static double Clamp(double value, double min, double max)
      => Math.Max(min, Math.Min(max, value));

    public ObservableCollection<ShowMessageModel> GetShowMessageModels()
    {
      return Messages;
    }

    /// <summary>
    /// Удаляет указанное количество последних строк из списка протокола.
    /// </summary>
    /// <param name="count">Количество строк для удаления. По умолчанию 1.</param>
    /// <returns>Фактическое количество удалённых строк.</returns>
    public Task<int> RemoveLastLinesAsync(int count = 1)
    {
      int removed = 0;

      Application.Current.Dispatcher.Invoke(() =>
      {
        int linesToRemove = Math.Min(count, Messages.Count);

        for (int i = 0; i < linesToRemove; i++)
        {
          Messages.RemoveAt(Messages.Count - 1);
          RemoveLastDisplayItem();
          removed++;
        }
      });

      return Task.FromResult(removed);
    }

    /// <summary>
    /// Полностью очищает протокол.
    /// </summary>
    public async Task ClearAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        Messages.Clear();
        DisplayItems.Clear();
        _currentGroup = null;
        _pendingCommandHeaderItem = null;

        LogInformation("Протокол полностью очищен.");
      });
    }

    /// <summary>
    /// Удаляет первую найденную строку, содержащую указанный текст.
    /// </summary>
    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove)
    {
      return await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        try
        {
          var target = Messages.FirstOrDefault(m =>
            (!string.IsNullOrEmpty(m.Header) && m.Header.Contains(textToRemove, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(m.Message) && m.Message.Contains(textToRemove, StringComparison.OrdinalIgnoreCase)));

          if (target != null)
          {
            Messages.Remove(target);
            RebuildDisplayItems();

            LogInformation($"Строка '{textToRemove}' найдена и удалена.");
            return true;
          }

          LogWarning($"Строка '{textToRemove}' не найдена.");
          return false;
        }
        catch (Exception ex)
        {
          LogException("Ошибка при удалении строки", ex);
          return false;
        }
      });
    }

    /// <summary>
    /// Добавляет новую строку в протокол.
    /// Если строка является командой, она открывает новый сворачиваемый блок.
    /// Если команда ещё не встречалась, обычные сообщения добавляются как корневые строки.
    /// </summary>
    public async Task AppendLineAsync(ShowMessageModel showMessageModel)
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {

        Messages.Add(showMessageModel);

        AppendToDisplayItems(showMessageModel);

        var lastRootItem = DisplayItems.LastOrDefault();
        if (lastRootItem != null)
        {
          ProtocolListBox.ScrollIntoView(lastRootItem);
        }
      });
    }

    /// <summary>
    /// Добавляет строки в иерархическую коллекцию отображения.
    /// </summary>
    private void AppendToDisplayItems(ShowMessageModel model)
    {
      if (model.Status == ShowMessageModel.MessageType.Command)
      {
        var commandItem = ProtocolDisplayItem.CreateLine(model);

        DisplayItems.Add(commandItem);

        _currentGroup = null;
        _pendingCommandHeaderItem = commandItem;
        return;
      }

      var lineItem = ProtocolDisplayItem.CreateLine(model);

      if (_pendingCommandHeaderItem != null)
      {
        int index = DisplayItems.IndexOf(_pendingCommandHeaderItem);

        if (index >= 0)
        {
          var group = ProtocolDisplayItem.CreateGroup(_pendingCommandHeaderItem.Message!);
          group.Children.Add(lineItem);

          DisplayItems[index] = group;
          _currentGroup = group;
        }
        else
        {
          DisplayItems.Add(lineItem);
          _currentGroup = null;
        }

        _pendingCommandHeaderItem = null;
        return;
      }

      if (_currentGroup != null)
      {
        _currentGroup.Children.Add(lineItem);
        return;
      }

      DisplayItems.Add(lineItem);
    }

    /// <summary>
    /// Удаляет последний элемент из иерархической коллекции отображения.
    /// </summary>
    private void RemoveLastDisplayItem()
    {
      if (DisplayItems.Count == 0)
      {
        _currentGroup = null;
        return;
      }

      var lastRoot = DisplayItems[^1];

      if (lastRoot.IsGroup && lastRoot.Children.Count > 0)
      {
        lastRoot.Children.RemoveAt(lastRoot.Children.Count - 1);
      }
      else
      {
        DisplayItems.RemoveAt(DisplayItems.Count - 1);
      }

      _currentGroup = DisplayItems.LastOrDefault(x => x.IsGroup);
    }

    /// <summary>
    /// Полностью пересобирает иерархическую коллекцию DisplayItems из плоского списка Messages.
    /// Нужна после удаления произвольной строки.
    /// </summary>
    private void RebuildDisplayItems()
    {
      DisplayItems.Clear();
      _currentGroup = null;
      _pendingCommandHeaderItem = null;

      foreach (var message in Messages)
      {
        AppendToDisplayItems(message);
      }
    }

    /// <summary>
    /// Добавляет пустую строку в протокол.
    /// </summary>
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

    /// <summary>
    /// Возвращает весь текст протокола в виде строки с учётом табуляции.
    /// </summary>
    /// <returns>Общий текст протокола.</returns>
    public string GetText()
    {
      return string.Join(Environment.NewLine, Messages.Select(m =>
      {
        string indent = new string(' ', m.IndentLevel * 2);
        string header = string.IsNullOrWhiteSpace(m.Header) ? string.Empty : $"{m.Header}: ";

        string timePart = string.IsNullOrWhiteSpace(m.Time)
          ? string.Empty
          : $" | {m.Time}";

        return $"{indent}{header}{m.Message}{timePart}";
      }));
    }

    /// <summary>
    /// Возвращает индекс последней строки протокола.
    /// </summary>
    public int GetLastLineNumber()
    {
      if (Messages.Count > 0)
      {
        return Messages.Count - 1;
      }

      return -1;
    }

    /// <summary>
    /// Прокручивает список к указанной строке.
    /// </summary>
    public async Task MoveToLineAsync(int lineNumber)
    {
      if (DisplayItems.Count > 0 && lineNumber >= 0 && lineNumber < DisplayItems.Count)
      {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
          var item = DisplayItems[lineNumber];
          ProtocolListBox.SelectedItem = item;
          ProtocolListBox.ScrollIntoView(item);
        });
      }
    }
  }
}