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

    /// <summary>
    /// Обработчик нажатия на кнопку "Архив", открывает окно работы с архивами.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Archive_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      //var allArchives = new TableAllArchivesControl();
      //AddControl(allArchives, "Архив");
      //allArchives.ArchiveSelected += ArchiveControl_ArchiveSelected;
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
          Title = "Выберите текстовый файл"
        };

        if (openFileDialog.ShowDialog() == true)
        {
          string filePath = openFileDialog.FileName;
          MultiWindow.OpenFileInEditor(filePath);
          LogInformation($"Файл открыт: {filePath}");
        }
      }
    }

    private void OnRequestShowProgress()
    {
      // Применяем блюр и блокируем главное окно
      this.Effect = new BlurEffect { Radius = 5 };
      this.IsEnabled = false;

      // Показываем окно прогресса (можно создать его на UI-потоке)
      // Пример: если у вас есть метод ShowProgressWindow()
      ShowProgressWindow();
    }

    private void OnRequestCloseProgress()
    {
      // Снимаем блюр и разблокируем главное окно
      this.Effect = null;
      this.IsEnabled = true;

      // Закрываем окно прогресса, если оно открыто
      CloseProgressWindow();
    }

    private void ShowProgressWindow()
    {
      if (_progressWindow == null)
      {
        _progressWindow = new ProgressWindow
        {
          Owner = this,
          WindowStartupLocation = WindowStartupLocation.CenterOwner,
          Topmost = true,
          ShowInTaskbar = false
        };
        _progressWindow.Show();
      }
    }

    private void CloseProgressWindow()
    {
      if (_progressWindow != null)
      {
        _progressWindow.Close();
        _progressWindow = null;
      }
    }

    private void SearchMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (_isSearchWindowOpen == false)
      {
        _searchWindow.Owner = this;

        EventAggregator.SearchText -= SearchWindow_SearchTextHandler;
        EventAggregator.SearchText += SearchWindow_SearchTextHandler;

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

    private void SearchWindow_SearchTextHandler(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      MultiWindow.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    private void OpenFileFromEvent()
    {
      OpenFile().ConfigureAwait(false);
    }

    private void CompareMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      MessageBox.Show("Нажата кнопка \"Сравнить\"", "Заглушка");
    }


    #endregion

    #region Самоконтроль.

    /// <summary>
    /// Добавляет элемент управления для самоконтроля одного из модулей в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void Test_Self(object sender, MouseButtonEventArgs e) => throw new NotImplementedException(); // await AddControlAsync(new Mode.SelfControl.Module.ModuleSelfControl(), "Самоконтроль модулей");

    #endregion

    #region Обработка событий.

    /// <summary>
    /// Обрабатывает изменения прав администратора, обновляя видимость соответствующих элементов интерфейса.
    /// </summary>
    private void ApplicationDataHandler_AdminRightsChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (newValue)
        {
          this.Admin.Visibility = Visibility.Visible;
          LogInformation("Права администратора предоставлены.");
        }
        else
        {
          this.Admin.Visibility = Visibility.Collapsed;
          LogInformation("Права администратора отозваны.");
        }
      });
    }

    /// <summary>
    /// Обрабатывает изменения состояния блокировки приложения, изменяя видимость панели и логируя изменения.
    /// </summary>
    private void ApplicationDataHandler_LockedChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (newValue)
        {
          TopPanel.Visibility = Visibility.Collapsed;
          isLocked = true;
          LogInformation("Приложение заблокировано.");
        }
        else
        {
          TopPanel.Visibility = Visibility.Visible;
          isLocked = false;
          LogInformation("Приложение разблокировано.");
        }
      });
    }

    #endregion
  }
}
