using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace UI.Components.ProtocolListBox
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

    /// <summary>
    /// Размер шрифта строк протокола.
    /// </summary>
    public static readonly DependencyProperty ProtocolFontSizeProperty =
      DependencyProperty.Register(
        nameof(ProtocolFontSize),
        typeof(double),
        typeof(ProtocolListBoxUI),
        new PropertyMetadata(DefaultFontSize));

    public double ProtocolFontSize
    {
      get => (double)GetValue(ProtocolFontSizeProperty);
      set => SetValue(ProtocolFontSizeProperty, value);
    }

    public ObservableCollection<ShowMessageModel> Messages { get; } = new();
    public string Header { get; set; }

    public bool HasRetryAction => throw new NotImplementedException();

    public bool ClickRetry { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public IButtonService ButtonService { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ProtocolListBoxUI()
    {
      InitializeComponent();
      PreviewKeyDown += ProtocolListBoxUI_PreviewKeyDown;
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
          // Вытаскиваем текст напрямую из Messages
          var text = GetText();
          TextPrintHelper.PrintText(text, "Печать протокола");
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка печати", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    /// <summary>
    /// Масштабирование текста протокола по Ctrl+колесо мыши.
    /// </summary>
    private void ProtocolListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
      {
        return;
      }

      if (e.Delta > 0)
      {
        Zoom(true);
      }
      else if (e.Delta < 0)
      {
        Zoom(false);
      }

      e.Handled = true;
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
          removed++;
        }
      });

      return Task.FromResult(removed);
    }

    public async Task ClearAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        Messages.Clear();
        LogInformation("Протокол полностью очищен.");
      });
    }

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

    public async Task AppendLineAsync(ShowMessageModel showMessageModel)
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        Messages.Add(showMessageModel);
        ProtocolListBox.ScrollIntoView(showMessageModel);
      });
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

    public async Task ShowMessageAsync(ShowMessageModel model, bool IsBlockStart = false, bool SkipStepModeCheck = false, bool skipPause = false,
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
        string header = string.IsNullOrWhiteSpace(m.Header) ? "" : $"{m.Header}: ";
        return $"{indent}{header}{m.Message} | {m.Time}";
      }));
    }

    public int GetLastLineNumber()
    {
      if (Messages.Count > 0)
      {
        return Messages.Count - 1;
      }
      else
      {
        return -1;
      }
    }

    public async Task MoveToLineAsync(int lineNumber)
    {
      // Проверьте, что Messages не пустая и lineNumber в пределах доступных индексов
      if (Messages.Count > 0 && lineNumber >= 0 && lineNumber < Messages.Count)
      {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
          ProtocolListBox.SelectedItem = Messages[lineNumber];
          var item = ProtocolListBox.Items.GetItemAt(lineNumber);
          ProtocolListBox.ScrollIntoView(item);
        });
      }
    }


  }
}
