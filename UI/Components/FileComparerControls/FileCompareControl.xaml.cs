using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.DTO.TextEditor.Ask.Core.Shared.DTO.TextEditor;
using Ask.Support;
using Message;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI.Controls.TextEditor;
using static Ask.LogLib.LoggerUtility;

namespace UI.Components.FileComparerControls
{
  /// <summary>
  /// Логика взаимодействия для FileCompareControl.xaml
  /// </summary>
  public partial class FileCompareControl : UserControl
  {
    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }

    private List<Dictionary<int, string>> _fileComparer;

    private bool isLeftPanelVisible = true;

    public FileCompareControl(string firstFilePath, string secondFilePath)
    {
      InitializeComponent();
      this.FirstFilePath = firstFilePath;
      this.SecondFilePath = secondFilePath;
      LoadFiles();

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "FuncCompare");
      };
    }

    private void LoadFiles()
    {
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

      this._fileComparer = FileCompare.CompareFileContents(this.FirstFilePath, this.SecondFilePath);

      if (this._fileComparer[0].Count == 0 && this._fileComparer[1].Count == 0)
      {
        MessageBoxCustom.Show("Отличия в файлах не найдены", "Отличия не найдены", MessageBoxButton.OK, MessageBoxImage.Warning);
        LogInformation($"Отличия в файлах {this.FirstFilePath} и {this.SecondFilePath} не найдены");
        return;
      }
      else
      {
        HighlightDifferences(this._fileComparer[0], this._fileComparer[1]);
      }
    }

    private void HighlightDifferences(Dictionary<int, string> leftDiffs, Dictionary<int, string> rightDiffs)
    {
      TextEditorUI leftEditor, rightEditor;
      PrepareTexyEditor(out leftEditor, out rightEditor);

      HighlightLines(leftEditor, leftDiffs.Keys, Colors.DarkRed);
      HighlightLines(rightEditor, rightDiffs.Keys, Colors.DarkRed);
    }

    private void PrepareTexyEditor(out TextEditorUI leftEditor, out TextEditorUI rightEditor)
    {
      leftEditor = HorizontalPanel.Visibility == Visibility.Visible ? TopBox : LeftBox;
      rightEditor = HorizontalPanel.Visibility == Visibility.Visible ? BottomBox : RightBox;
      leftEditor.MarkerService.ClearAllMarkers();
      rightEditor.MarkerService.ClearAllMarkers();
    }

    private void HighlightLines(TextEditorUI editor, IEnumerable<int> lineNumbers, Color color)
    {
      foreach (var lineNumber in lineNumbers)
      {
        TextLine? docLine = editor.Document.GetLine(lineNumber + 1);

        if (docLine is TextLine line)
        {
          editor.MarkerService.AddMarker(line.Offset, line.Length, color);
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
        HighlightDifferences(this._fileComparer[0], this._fileComparer[1]);
      }
      else
      {
        (LeftBox.Text, RightBox.Text, FirstVerticalFileName.Text, SecondVerticalFileName.Text) =
          (TopBox.Text, BottomBox.Text, FirstFileName.Text, SecondFileName.Text);
        HighlightDifferences(this._fileComparer[0], this._fileComparer[1]);
      }
    }

    private void ChangeFile_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var newFilePath = ChangeFile();
      if (!string.IsNullOrEmpty(newFilePath) && File.Exists(newFilePath))
      {
        bool changeFirstFile = sender == ChangeFirstFile;
        if (!string.Equals(this.FirstFilePath, newFilePath) && !string.Equals(this.SecondFilePath, newFilePath))
        {

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
        else
        {
          MessageBoxCustom.Show("Вы уже выбрали этот файл для сравнения", "Неверный путь к файлу", MessageBoxButton.OK, MessageBoxImage.Warning);
          LogWarning("Попытка сравнить один и тот же файл");
        }
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

    private void CompareFiles_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      TextEditorUI firstEditor, secondEditor;
      PrepareTexyEditor(out firstEditor, out secondEditor);
      var firstText = firstEditor.Document.Text;
      var secondText = secondEditor.Document.Text;
      var firstLines = firstText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
      var secondLines = secondText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
      this._fileComparer.Clear();
      this._fileComparer = FileCompare.CompareLineArrays(firstLines, secondLines);

      if (this._fileComparer[0].Count == 0 && this._fileComparer[1].Count == 0)
      {
        MessageBoxCustom.Show("Отличия в файлах не найдены", "Отличия не найдены", MessageBoxButton.OK, MessageBoxImage.Information);
        LogInformation($"Отличия в открытых файлах не найдены");
        return;
      }
      else
      {
        HighlightDifferences(this._fileComparer[0], this._fileComparer[1]);
      }
    }

    private async void MenuButton_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
    {
      isLeftPanelVisible = !isLeftPanelVisible;

      if (!isLeftPanelVisible)
      {
        int newWidth = 50;
        while (PanelManagment.Width.Value > newWidth)
        {
          PanelManagment.Width = new GridLength(PanelManagment.Width.Value - 25);
          if (ButtonsGrid.Opacity > 0)
          {
            ButtonsGrid.Opacity -= 0.1;
          }
          await Task.Delay(1);
        }
        ButtonsGrid.Opacity = 0;
      }
      else
      {
        int newWidth = 250;
        while (PanelManagment.Width.Value < newWidth)
        {
          PanelManagment.Width = new System.Windows.GridLength(PanelManagment.Width.Value + 25);
          if (ButtonsGrid.Opacity < 1)
          {
            ButtonsGrid.Opacity += 0.1;
          }
          await Task.Delay(1);
        }

        PanelManagment.Width = new GridLength(250);
        ButtonsGrid.Opacity = 1;
      }
    }

  }
}
