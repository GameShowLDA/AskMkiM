using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace UI.Controls.Settings.AskMkiConfig;

/// <summary>
/// Панель редактирования группы параметров legacy-конфигурации АСК-МКИ в правом drawer.
/// </summary>
public partial class AskMkiGroupEditorDrawerControl : UserControl
{
  /// <summary>
  /// Инициализирует новый экземпляр класса <see cref="AskMkiGroupEditorDrawerControl"/>.
  /// </summary>
  public AskMkiGroupEditorDrawerControl()
  {
    InitializeComponent();
  }

  /// <summary>
  /// Возникает при запросе сохранения параметров.
  /// </summary>
  public event EventHandler? SaveRequested;

  /// <summary>
  /// Возникает при запросе отмены изменений.
  /// </summary>
  public event EventHandler? CancelRequested;

  /// <summary>
  /// Обрабатывает нажатие кнопки сохранения.
  /// </summary>
  private void SaveButton_Click(object sender, RoutedEventArgs e)
  {
    SaveRequested?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  /// Обрабатывает нажатие кнопки отмены.
  /// </summary>
  private void CancelButton_Click(object sender, RoutedEventArgs e)
  {
    CancelRequested?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  /// Ограничивает ввод номера БК двумя цифрами.
  /// </summary>
  private void SmallNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
  {
    if (sender is not TextBox textBox)
    {
      return;
    }

    var newText = GetProposedText(textBox, e.Text);
    e.Handled = newText.Length > 2 || newText.Any(ch => !char.IsDigit(ch));
  }

  /// <summary>
  /// Проверяет вставляемое значение номера БК.
  /// </summary>
  private void SmallNumberTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
  {
    if (!e.DataObject.GetDataPresent(DataFormats.Text) || sender is not TextBox textBox)
    {
      e.CancelCommand();
      return;
    }

    var pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
    var newText = GetProposedText(textBox, pastedText);

    if (newText.Length > 2 || newText.Any(ch => !char.IsDigit(ch)))
    {
      e.CancelCommand();
    }
  }

  /// <summary>
  /// Возвращает текст поля после предполагаемого ввода или вставки.
  /// </summary>
  private static string GetProposedText(TextBox textBox, string newTextPart)
  {
    var currentText = textBox.Text ?? string.Empty;

    if (textBox.SelectionLength > 0)
    {
      currentText = currentText.Remove(textBox.SelectionStart, textBox.SelectionLength);
    }

    return currentText.Insert(textBox.SelectionStart, newTextPart);
  }
}
