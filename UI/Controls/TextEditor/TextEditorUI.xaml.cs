using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI.Components;

namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Логика взаимодействия для TextEditorUI.xaml.
  /// </summary>
  public partial class TextEditorUI : UserControl
  {
    /// <summary>
    /// Экземпляр <see cref="MultiEditorControl"/>, используемый для работы с вкладками редактора.
    /// </summary>
    MultiEditorControl _multiEditorControl;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextEditorUI"/>.
    /// </summary>
    public TextEditorUI()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Устанавливает ссылку на <see cref="MultiEditorControl"/> для управления файлами.
    /// </summary>
    /// <param name="multiEditorControl">Экземпляр <see cref="MultiEditorControl"/>.</param>
    public void SetMultiEditorControl(MultiEditorControl multiEditorControl)
    {
      _multiEditorControl = multiEditorControl;
    }

    /// <summary>
    /// Обработчик события DragEnter. Меняет фон текстового редактора при наведении файла.
    /// </summary>
    private void textEditor_DragEnter(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        textEditor.Background = (Brush)FindResource("ActiveBorderSolidColorBrush");
        e.Effects = DragDropEffects.Copy;
      }
      else
      {
        e.Effects = DragDropEffects.None;
      }
    }

    /// <summary>
    /// Обработчик события DragLeave. Восстанавливает исходный фон редактора.
    /// </summary>
    private void textEditor_DragLeave(object sender, DragEventArgs e)
    {
      textEditor.Background = (Brush)FindResource("PrimarySolidColorBrush");
    }

    /// <summary>
    /// Обработчик события Drop. Загружает содержимое перетаскиваемого файла в редактор.
    /// </summary>
    private void textEditor_Drop(object sender, DragEventArgs e)
    {
      textEditor.Background = (Brush)FindResource("PrimarySolidColorBrush");

      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files.Length > 0)
        {
          string filePath = files[0];
          try
          {
            if (_multiEditorControl == null)
            {
              string content = System.IO.File.ReadAllText(filePath);
              textEditor.Text = content;
            }
            else
            {
              _multiEditorControl.OpenFile(filePath);
            }
          }
          catch (Exception ex)
          {
            MessageBox.Show($"Ошибка при открытии файла: {ex.Message}");
          }
        }
      }
    }

    /// <summary>
    /// Получает или задает текст в текстовом редакторе.
    /// </summary>
    public string Text { get { return textEditor.Text; } set { textEditor.Text = value; } }
  }
}
