using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Mode.Metrology.CI;
using System.Windows.Controls;
using UI.Controls.Search;
using UI.Components;
using UI.Controls.TextEditor;
using System.Windows.Media.Effects;
using Mode.Metrology.IE;
using Mode.Metrology.KC;
using Mode.Metrology.PI;
using Mode.TestSuite.Metrology.MethodExecutor.CI;
using Mode.TestSuite.Metrology.MethodExecutor.PI;
using Mode.TestSuite.Metrology.NodeMethod.CI;
using Mode.TestSuite.Metrology.NodeMethod.PI;
using static AppConfiguration.SystemState.SystemStateManager;
using static Utilities.LoggerUtility;
using AppConfiguration.Base;
using static UI.Components.Invoke.OpenFileButton;
using Mode.Metrology.PR;

namespace MainWindowProgram
{
  public partial class MainWindow
  {
    public bool _isSearchWindowOpen;
    private SearchWindow _searchWindow;
    private ProgressWindow _progressWindow;

    #region Файл.

    #region УБРАТЬ ОТ СЮДА!!!
    private void OpenFileFromEvent()
    {
      OpenFile().ConfigureAwait(false);
    }
    private async Task OpenFile()
    {
      if (await GetIsLocked())
      {
        MessageBox.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        LogWarning("Попытка открыть файл, когда приложение заблокировано.");
      }
      else
      {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
          Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|RTF files (*.rtf)|*.rtf",
          Title = "Выберите текстовый файл",
        };

        if (openFileDialog.ShowDialog() == true)
        {
          string filePath = openFileDialog.FileName;
          MultiWindow.OpenFileInEditor(filePath);
          LogInformation($"Файл открыт: {filePath}");
        }
      }
    } 
    #endregion

    private void SearchMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (_isSearchWindowOpen == false)
      {
        _searchWindow.Owner = this;

        EventAggregator.SearchText -= ApplicationEvents.UiEvents.SearchWindow_SearchTextHandler;
        EventAggregator.SearchText += ApplicationEvents.UiEvents.SearchWindow_SearchTextHandler;

        _searchWindow.SelectFileForSearch -= OpenFileFromEvent;
        _searchWindow.SelectFileForSearch += OpenFileFromEvent;

        TextEditorUI activeEditor = MultiWindow.GetActiveTextEditor();
        string selectedText = activeEditor?.TextArea.Selection.GetText();

        if (!string.IsNullOrEmpty(selectedText))
        {
          EventAggregator.RaiseSearchTextRequested(selectedText);
        }

        _searchWindow.ShowWindow();
        _searchWindow.ClearHighlights -= MultiWindow.OnSearchWindowClosing;
        _searchWindow.ClearHighlights += MultiWindow.OnSearchWindowClosing;
        
        _isSearchWindowOpen = true;

        var temp = _searchWindow.FindName("SearchTextBox") as TextBox;
        LogInformation($"Открыто окно поиска. Текст в строке поиска: {temp.Text}");
      }
    }



    private void CompareMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      MessageBox.Show("Нажата кнопка \"Сравнить\"", "Заглушка");
    }

    #endregion
  }
}
