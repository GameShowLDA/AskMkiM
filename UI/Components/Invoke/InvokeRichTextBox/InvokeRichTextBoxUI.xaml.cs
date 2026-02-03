using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace UI.Components.Invoke.InvokeRichTextBox
{
  /// <summary>
  /// Логика взаимодействия для InvokeRichTextBoxUI.xaml.
  /// </summary>
  public partial class InvokeRichTextBoxUI : UserControl
  {
    private bool _isCtrlPressed;

    /// <summary>
    /// Текущий размер шрифта.
    /// </summary>
    private double _currentFontSize = 15;

    /// <summary>
    /// Получение или установка значения, определяющего доступность элемента управления, предназначенного для редактирование текста, только для чтения.
    /// </summary>
    public bool IsReadOnly
    {
      get
      {
        bool readOnly = false;
        Application.Current.Dispatcher.Invoke(() => readOnly = protocolTextBox.IsReadOnly);
        return readOnly;
      }

      set
      {
        Application.Current.Dispatcher.Invoke(new Action(() => protocolTextBox.IsReadOnly = value));
      }
    }

    /// <summary>
    ///  Возвращает или задает System.Windows.Documents.FlowDocument представляющий содержимое System.Windows.Controls.RichTextBox.
    /// </summary>
    public FlowDocument Document
    {
      get
      {
        FlowDocument document = default;
        Application.Current.Dispatcher.Invoke(() => document = protocolTextBox.Document);
        return document;
      }
    }

    /// <summary>
    /// Прокручивает содержимое TextBox до конца.
    /// </summary>
    public void ScrollToEnd()
    {
      Application.Current.Dispatcher.Invoke(new Action(() => protocolTextBox.ScrollToEnd()));
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Ваш класс"/>.
    /// </summary>
    public InvokeRichTextBoxUI()
    {
      InitializeComponent();
      protocolTextBox.KeyDown += ProtocolTextBox_KeyDown;
      protocolTextBox.KeyUp += ProtocolTextBox_KeyUp;
      protocolTextBox.PreviewMouseWheel += ProtocolTextBox_PreviewMouseWheel;
    }

    /// <summary>
    /// Выводит информацию в текстовое окно.
    /// </summary>
    /// <param name="header">Заголовок.</param>
    /// <param name="headerColor">Цвет заголовка.</param>
    /// <param name="description">Описание.</param>
    /// <param name="descriptionColor">Цвет описания.</param>
    /// <returns></returns>
    public async Task ShowMessageAsync(ShowMessageModel showMessageModel)
    {
      await AppendLineAsync(showMessageModel);
    }

    /// <summary>
    /// Добавляет параграф с текстом в RichTextBox.
    /// </summary>
    /// <param name="paragraph">Параграф, который необходимо добавить.</param>
    public void AppendParagraph(Paragraph paragraph)
    {
      StringBuilder text = new StringBuilder();
      foreach (var inline in paragraph.Inlines)
      {
        if (inline is Run run)
        {
          text.Append(run.Text);
        }
      }

      LogInformation($"Отобразить текст: {text.ToString()}");
      protocolTextBox.Document.Blocks.Add(paragraph);
    }

    /// <summary>
    /// Асинхронно добавляет строку в RichTextBox.
    /// </summary>
    /// <param name="header">Заголовок.</param>
    /// <param name="description">Описание.</param>
    /// <param name="headerColor">Цвет заголовка.</param>
    /// <param name="descriptionColor">Цвет описания.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task AppendLineAsync(ShowMessageModel showMessageModel)
    {
      (string header, Color? headerColor, Color? descriptionColor) = SetDefaultValues(showMessageModel.Header, showMessageModel.HeaderColor, showMessageModel.MessageColor);

      await Application.Current.Dispatcher.InvokeAsync(async () =>
      {
        Paragraph paragraph = await CreateParagraphAsync(showMessageModel);
        this.AppendParagraph(paragraph);
        protocolTextBox.ScrollToEnd();
      });
    }

    /// <summary>
    /// Асинхронно удаляет указанное количество последних строк из RichTextBox.
    /// </summary>
    /// <param name="count">Количество строк для удаления. По умолчанию 1.</param>
    /// <returns>Количество фактически удаленных строк.</returns>
    public async Task<int> RemoveLastLinesAsync(int count = 1)
    {
      return await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        try
        {
          var blocks = protocolTextBox.Document.Blocks.ToList();
          if (!blocks.Any())
          {
            return 0;
          }

          int linesToRemove = Math.Min(count, blocks.Count);
          for (int i = 0; i < linesToRemove; i++)
          {
            protocolTextBox.Document.Blocks.Remove(blocks[blocks.Count - 1 - i]);
          }

          protocolTextBox.ScrollToEnd();
          return linesToRemove;
        }
        catch (Exception ex)
        {
          LogException($"Ошибка при удалении строк", ex);
          return 0;
        }
      });
    }

    /// <summary>
    /// Асинхронно удаляет блок, содержащий указанную строку или её часть, из RichTextBox.
    /// </summary>
    /// <param name="textToRemove">Строка для поиска и удаления.</param>
    /// <returns>True, если блок был найден и удален; иначе False.</returns>
    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove)
    {
      return await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        try
        {
          var blocks = protocolTextBox.Document.Blocks.ToList();
          foreach (var block in blocks)
          {
            if (block is Paragraph paragraph)
            {
              var text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;
              if (text.Contains(textToRemove))
              {
                protocolTextBox.Document.Blocks.Remove(block);
                protocolTextBox.ScrollToEnd();
                LogInformation($"Строка '{textToRemove}' найдена и удалена.");
                return true;
              }
            }
          }

          // Если полное совпадение не найдено, ищем часть строки
          foreach (var block in blocks)
          {
            if (block is Paragraph paragraph)
            {
              var text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;
              if (text.Contains(textToRemove.Substring(0, Math.Min(20, textToRemove.Length)))) // Ищем часть строки
              {
                protocolTextBox.Document.Blocks.Remove(block);
                protocolTextBox.ScrollToEnd();
                LogInformation($"Часть строки '{textToRemove}' найдена и удалена.");
                return true;
              }
            }
          }

          LogWarning($"Строка '{textToRemove}' не найдена.");
          return false;
        }
        catch (Exception ex)
        {
          LogException($"Ошибка при удалении строки", ex);
          return false;
        }
      });
    }

    /// <summary>
    /// Создает параграф с заголовком, описанием и временем выполнения (если включено).
    /// </summary>
    private async Task<Paragraph> CreateParagraphAsync(ShowMessageModel showMessageModel)
    {
      Paragraph paragraph = new Paragraph
      {
        LineHeight = 2,
        Foreground = new SolidColorBrush(Colors.White)
      };

      // Формируем отступ — 2 пробела на каждый уровень
      string indent = new string(' ', showMessageModel.IndentLevel * 2);
      try
      {
        Run headerRun = new Run(indent + showMessageModel.Header)
        {
          FontSize = _currentFontSize,
          Foreground = new SolidColorBrush(showMessageModel.HeaderColor.Value)
        };

        paragraph.Inlines.Add(headerRun);

        if (!string.IsNullOrEmpty(showMessageModel.Message))
        {
          paragraph.Inlines.Add(new Run(":  "));
          Run descriptionRun = new Run(showMessageModel.Message) { FontSize = _currentFontSize, Foreground = new SolidColorBrush(showMessageModel.MessageColor ?? Colors.White) };
          paragraph.Inlines.Add(descriptionRun);
        }

        if (ProtocolConfig.GetTimeStart())
        {
          string elapsedTime = SystemStateManager._stopwatch.Elapsed.ToString(@"mm\:ss\.fff", System.Globalization.CultureInfo.InvariantCulture);
          paragraph.Inlines.Add(new Run("  ["));
          Run timeWatch = new Run(elapsedTime) { FontSize = _currentFontSize, Foreground = new SolidColorBrush(Colors.YellowGreen) };
          paragraph.Inlines.Add(timeWatch);
          paragraph.Inlines.Add(new Run("]"));
        }

        if (ExecutionConfig.GetIsIdleModeEnabled() && showMessageModel.IsDeviceMessage)
        {
          string idleText = " | Холостой режим";
          Run run = new Run(idleText) { FontSize = _currentFontSize, Foreground = new SolidColorBrush(showMessageModel.HeaderColor.Value) };
          paragraph.Inlines.Add(run);
        }
      }
      catch (Exception ex)
      {
        LogError(ex.Message.ToString());
      }

      return paragraph;
    }

    /// <summary>
    /// Устанавливает значения по умолчанию для параметров.
    /// </summary>
    private (string header, Color? headerColor, Color? descriptionColor) SetDefaultValues(string header, Color? headerColor, Color? descriptionColor)
    {
      return
      (
        header ?? string.Empty,
        headerColor ?? Colors.White,
        descriptionColor ?? Colors.White
      );
    }

    /// <summary>
    /// Обрабатывает событие KeyDown для ProtocolTextBox. Устанавливает _isCtrlPressed в true, если нажата любая клавиша Ctrl.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события.</param>
    private void ProtocolTextBox_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
      {
        _isCtrlPressed = true;
      }
    }

    /// <summary>
    /// Обрабатывает событие KeyUp для ProtocolTextBox. Устанавливает _isCtrlPressed в false, если отпущена любая клавиша Ctrl.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события.</param>
    private void ProtocolTextBox_KeyUp(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
      {
        _isCtrlPressed = false;
      }
    }

    /// <summary>
    /// Обрабатывает событие PreviewMouseWheel для ProtocolTextBox. Изменяет размер шрифта текстового поля, если нажата клавиша Ctrl.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события.</param>
    private void ProtocolTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (_isCtrlPressed)
      {
        if (e.Delta > 0)
        {
          ChangeFontSize(2);
        }
        else if (e.Delta < 0)
        {
          ChangeFontSize(-2);
        }

        e.Handled = true;
      }
    }

    /// <summary>
    /// Асинхронно очищает все строки в RichTextBox.
    /// </summary>
    /// <returns>Асинхронная задача.</returns>
    public async Task ClearAsync()
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        protocolTextBox.Document.Blocks.Clear();
        protocolTextBox.ScrollToEnd();
        LogInformation("Протокол полностью очищен.");
      });
    }

    /// <summary>
    /// Изменяет размер шрифта всего текста в _richTextBox на указанное значение.
    /// </summary>
    /// <param name="change">Величина, на которую изменяется размер шрифта.</param>
    public void ChangeFontSize(double change)
    {
      if (_currentFontSize + change > 0)
      {
        _currentFontSize += change;
      }

      foreach (Block block in protocolTextBox.Document.Blocks)
      {
        if (block is Paragraph paragraph)
        {
          foreach (Inline inline in paragraph.Inlines)
          {
            if (inline.FontSize + change > 0)
            {
              inline.FontSize = _currentFontSize;
            }
          }
        }
      }
    }
  }
}
