using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using UI.Components;
using UI.Components.SearchControls;

namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Логика взаимодействия для TextEditorUI.xaml
  /// </summary>
  public partial class TextEditorUI : UserControl
  {
    MultiEditorControl _multiEditorControl;
    private TextMarkerService textMarkerService;
    public ICSharpCode.AvalonEdit.TextEditor TextEditor => textEditor;

    public TextEditorUI()
    {
      InitializeComponent();
      textMarkerService = new TextMarkerService(textEditor.Document, textEditor);

      textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
      textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
    }

    public void SetMultiEditorControl(MultiEditorControl multiEditorControl)
    {
      _multiEditorControl = multiEditorControl;
    }

    public void ClearHighlights()
    {
      textMarkerService.RemoveAll();
    }

    public TextDocument Document => textEditor.Document;
    public TextArea TextArea => textEditor.TextArea;
    public void ScrollToLine(int line)
    {
      textEditor.ScrollToLine(line);
    }

    public void Select(int startOffset, int length)
    {
      textEditor.Select(startOffset, length);
    }

    private void textEditor_DragEnter(object sender, DragEventArgs e)
    {
      // Проверяем, перетаскиваются ли файлы
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        // Меняем фон текстового редактора на цвет подсветки
        textEditor.Background = (Brush)FindResource("ActiveBorderSolidColorBrush");
        e.Effects = DragDropEffects.Copy; // Устанавливаем эффект копирования
      }
      else
      {
        e.Effects = DragDropEffects.None; // Отменяем эффект, если это не файлы
      }
    }

    private void textEditor_DragLeave(object sender, DragEventArgs e)
    {
      // Возвращаем фон текстового редактора к исходному цвету
      textEditor.Background = (Brush)FindResource("PrimarySolidColorBrush");
    }

    private void textEditor_Drop(object sender, DragEventArgs e)
    {
      // Возвращаем фон текстового редактора к исходному цвету при отпускании файла
      textEditor.Background = (Brush)FindResource("PrimarySolidColorBrush");

      // Проверяем, перетаскиваются ли файлы
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        // Открываем первый файл
        if (files.Length > 0)
        {
          string filePath = files[0];
          try
          {
            // Загружаем содержимое файла в текстовый редактор
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

    public string Text { get { return textEditor.Text; } set { textEditor.Text = value; } }
  }
}
