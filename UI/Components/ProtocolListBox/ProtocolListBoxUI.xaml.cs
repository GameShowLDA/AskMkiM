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

    private async void ProtocolListBoxUI_PreviewKeyDown(object sender, KeyEventArgs e)
    {
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
        var displayLines = ExpandMessageForDisplay(showMessageModel);

        foreach (var line in displayLines)
        {
          Messages.Add(line);
        }

        ProtocolListBox.ScrollIntoView(displayLines.LastOrDefault() ?? showMessageModel);
      });
    }

    /// <summary>
    /// Разбивает многострочный заголовок на отдельные строки для корректной заливки фона.
    /// Пустые строки сохраняются, но отображаются без подсветки.
    /// </summary>
    private static IReadOnlyList<ShowMessageModel> ExpandMessageForDisplay(ShowMessageModel source)
    {
      if (!source.HeaderBackgroundColor.HasValue || string.IsNullOrEmpty(source.Header))
      {
        return new[] { source };
      }

      string normalized = source.Header.Replace("\r\n", "\n").Replace('\r', '\n');
      if (!normalized.Contains('\n'))
      {
        return new[] { source };
      }

      string[] headerLines = normalized.Split('\n');
      var result = new List<ShowMessageModel>(headerLines.Length);

      for (int i = 0; i < headerLines.Length; i++)
      {
        string line = headerLines[i];
        bool hasText = !string.IsNullOrWhiteSpace(line);

        var model = CloneForDisplay(source);
        model.Header = hasText ? line : " ";
        model.Message = string.Empty;
        model.Time = string.Empty;
        model.Debug = string.Empty;
        model.HeaderBackgroundColor = hasText ? source.HeaderBackgroundColor : null;

        if (!hasText)
        {
          model.HeaderColor = Colors.Transparent;
          model.MessageColor = Colors.Transparent;
        }

        result.Add(model);
      }

      // Хвост сообщения (message/time/debug) привязываем к последней строке.
      var last = result[^1];
      last.Message = source.Message;
      last.Time = source.Time;
      last.Debug = source.Debug;

      return result;
    }

    /// <summary>
    /// Создаёт копию сообщения для построчного отображения в списке.
    /// </summary>
    private static ShowMessageModel CloneForDisplay(ShowMessageModel source)
    {
      var clone = new ShowMessageModel
      {
        Status = source.Status,
        Header = source.Header,
        Message = source.Message,
        Time = source.Time,
        Debug = source.Debug,
        HeaderColor = source.HeaderColor,
        HeaderBackgroundColor = source.HeaderBackgroundColor,
        MessageColor = source.MessageColor,
        TimeColor = source.TimeColor,
        ExecutionError = source.ExecutionError,
        CanBeDeleted = source.CanBeDeleted,
        IsDeviceMessage = source.IsDeviceMessage,
        IsControlProgramCommandHeader = source.IsControlProgramCommandHeader,
        IndentLevel = source.IndentLevel
      };

      return clone;
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
