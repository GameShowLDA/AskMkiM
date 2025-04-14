using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media;
using UI.Controls.TextEditor;

namespace UI.Components.FileComparerControls
{
  /// <summary>
  /// Логика взаимодействия для FileCompareControl.xaml
  /// </summary>
  public partial class FileCompareControl : UserControl
  {
    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }
    public FileCompareControl(string firstFilePath, string secondFilePath)
    {
      InitializeComponent();
      this.FirstFilePath = firstFilePath;
      this.SecondFilePath = secondFilePath;
      LoadFiles();
    }

    private void LoadFiles()
    {
      // TODO: Заменить на сравнение в дальнейшем

      var firstFileText = File.ReadAllText(this.FirstFilePath);
      var secondFileText = File.ReadAllText(this.SecondFilePath);
      if (HorizontalPanel.Visibility == Visibility.Visible)
      {
        TopBox.Text = firstFileText;
        BottomBox.Text = secondFileText;
        FirstFileName.Text = Path.GetFileName(this.FirstFilePath);
        SecondFileName.Text = Path.GetFileName(this.SecondFilePath);
      }
      else
      {
        LeftBox.Text = firstFileText;
        RightBox.Text = secondFileText;
        FirstVerticalFileName.Text = Path.GetFileName(this.FirstFilePath);
        SecondVerticalFileName.Text = Path.GetFileName(this.SecondFilePath);
      }
      var fileComparer = FileCompare.CompareFileContents(this.FirstFilePath, this.SecondFilePath);
      HighlightDifferences(fileComparer[0], fileComparer[1]);

    }

    private void HighlightDifferences(Dictionary<int, string> leftDiffs, Dictionary<int, string> rightDiffs)
    {
      var leftEditor = HorizontalPanel.Visibility == Visibility.Visible ? TopBox : LeftBox;
      var rightEditor = HorizontalPanel.Visibility == Visibility.Visible ? BottomBox : RightBox;

      // Очистим предыдущие маркеры
      leftEditor.MarkerService.ClearAllMarkers();
      rightEditor.MarkerService.ClearAllMarkers();

      HighlightLines(leftEditor, leftDiffs.Keys, Colors.DarkRed);
      HighlightLines(rightEditor, rightDiffs.Keys, Colors.DarkRed);
    }

    private void HighlightLines(TextEditorUI editor, IEnumerable<int> lineNumbers, Color color)
    {
      foreach (var lineNumber in lineNumbers)
      {
        var docLine = editor.Document.GetLineByNumber(lineNumber + 1); // AvalonEdit: строки с 1, не с 0
        if (docLine != null)
        {
          editor.MarkerService.AddMarker(docLine.Offset, docLine.Length, color);
        }
      }
    }


    private void ToggleOrientation(object sender, MouseButtonEventArgs e)
    {
      bool toVertical = sender == LeftRight;

      HorizontalPanel.Visibility = toVertical ? Visibility.Collapsed : Visibility.Visible;
      VerticalPanel.Visibility = toVertical ? Visibility.Visible : Visibility.Collapsed;

      UpDown.Visibility = toVertical ? Visibility.Visible : Visibility.Collapsed;
      LeftRight.Visibility = toVertical ? Visibility.Collapsed : Visibility.Visible;

      if (HorizontalPanel.Visibility == Visibility.Visible)
      {
        (TopBox.Text, BottomBox.Text, FirstFileName.Text, SecondFileName.Text) = 
          (LeftBox.Text, RightBox.Text, FirstVerticalFileName.Text, SecondVerticalFileName.Text);
      }
      else
      {
        (LeftBox.Text, RightBox.Text, FirstVerticalFileName.Text, SecondVerticalFileName.Text) = 
          (TopBox.Text, BottomBox.Text, FirstFileName.Text, SecondFileName.Text);
      }
    }

    private void ChangeFile_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var newFilePath = ChangeFile();
      if (!string.IsNullOrEmpty(newFilePath) && File.Exists(newFilePath))
      {
        bool changeFirstFile = sender == ChangeFirstFile;
        if (changeFirstFile)
        {
          this.FirstFilePath = newFilePath;
        }
        else
        {
          this.SecondFilePath = newFilePath;
        }
        LoadFiles();
      }
    }

    private string ChangeFile()
    {
      OpenFileDialog openFileDialog = new OpenFileDialog
      {
        Title = "Выберите файл",
        Filter = "Text files (*.txt)|*.txt|RTF files (*.rtf)|*.rtf|PK files (*.pk;*.Pk;*.PK)|*.pk;*.Pk;*.PK|All files (*.*)|*.*",
        Multiselect = false
      };

      if (openFileDialog.ShowDialog() == true)
      {
        string filePath = openFileDialog.FileName;

        return filePath;
      }
      return string.Empty;
    }
  }
}
