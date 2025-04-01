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
    private SearchWindow _searchWindow = new SearchWindow();
    private ProgressWindow _progressWindow;

    #region Основные события управления окном.

    /// <summary>
    /// Обработчик нажатия на кнопку "Максимизировать", изменяет состояние окна между нормальным и максимизированным.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void maximizeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (this.WindowState != WindowState.Maximized)
      {
        this.WindowState = WindowState.Maximized;
      }
      else
      {
        this.WindowState = WindowState.Normal;
      }
    }

    /// <summary>
    /// Обработчик нажатия на кнопку "Минимизировать", сворачивает окно.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void minimizeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e) => this.WindowState = WindowState.Minimized;

    /// <summary>
    /// Обработчик нажатия на кнопку "Закрыть", завершает работу приложения.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void exitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown();

    /// <summary>
    /// Обработчик перетаскивания окна при нажатии и удерживании левой кнопки мыши на главном меню.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void mainMenu_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => this.DragMove();

    /// <summary>
    /// Обработчик перетаскивания окна при нажатии и удерживании левой кнопки мыши на верхней панели.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => this.DragMove();

    private async void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      var maxWidth = this.ActualWidth - mainMenu.ActualWidth - 50;
      var minWidth = 50 + ButtonsPanel.ActualWidth + mainMenu.ActualWidth;

      double minFontSize = 11;
      double maxFontSize = 15;
      double minWindowWidth = 300;
      double maxWindowWidth = 800;

      double fontSize = minFontSize + (maxFontSize - minFontSize) *
                        ((maxWidth - minWindowWidth) / (maxWindowWidth - minWindowWidth));

      fontSize = Math.Clamp(fontSize, minFontSize, maxFontSize);

      mainMenu.FontSize = fontSize;
    }

    #endregion

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

    /// <summary>
    /// Обработчик нажатия на кнопку "Открыть", открывает диалоговое окно для выбора текстового файла и загружает его в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Open_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
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

    /// <summary>
    /// Обработчик создания нового файла в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void Create_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
    {
      if (await GetIsLocked())
      {
        MessageBox.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        LogWarning("Попытка создать новый файл, когда приложение заблокировано.");
      }
      else
      {
        MultiWindow.CreateNewFile();
        LogInformation("Создан новый файл.");
      }
    }


    private void SaveMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      MultiWindow.SaveFile();
    }

    private void SaveAsMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      MultiWindow.SaveFileAs();
    }

    private void PrintMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      MultiWindow.PrintFile();
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

    /// <summary>
    /// Закрывает ПО.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Exit_Handler(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown();

    #endregion

    #region Метрология.

    /// <summary>
    /// Добавляет пользовательский элемент управления режима КС в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void KC_Handler(object sender, MouseButtonEventArgs e)
    {
      Application.Current.Dispatcher.Invoke(async () =>
      {
        await AddControlAsync(new KcMetrologyControl(), "Режим КС", TypeWindow.DeviceControl);
      });
    }

    /// <summary>
    /// Добавляет пользовательский элемент управления режима ИЕ в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void IE_Handler(object sender, MouseButtonEventArgs e) => await AddControlAsync(new IeMetrologyControl(), "Режим ИЕ", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет пользовательский элемент управления режима СИ в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void CI_Handler(object sender, MouseButtonEventArgs e) => await AddControlAsync(new CiMetrologyControl(), "Режим СИ", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет пользовательский элемент управления режима ПР в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void Pr_Handler(object sender, MouseButtonEventArgs e) => await AddControlAsync(new PrMetrologyControl(), "Режим ПР", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет пользовательский элемент управления режима ПИ(DCW) в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void PIDCW_Handler(object sender, MouseButtonEventArgs e) => await AddControlAsync(new PiDCWMetrologyControl(), "Режим ПИ(DCW)", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет пользовательский элемент управления режима ПИ(ACW) в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void PIACW_Handler(object sender, MouseButtonEventArgs e) => await AddControlAsync(new PiACWMetrologyControl(), "Режим ПИ(ACW)", TypeWindow.DeviceControl);

    #endregion

    #region Самоконтроль.

    /// <summary>
    /// Добавляет элемент управления для самоконтроля одного из модулей в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void Test_Self(object sender, MouseButtonEventArgs e) => throw new NotImplementedException(); // await AddControlAsync(new Mode.SelfControl.Module.ModuleSelfControl(), "Самоконтроль модулей");

    #endregion

    #region Тесты.

    /// <summary>
    /// Добавляет элемент управления для теста методом узла СИ в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void CiNodeMethodControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await AddControlAsync(new CiNodeMethodControl(), "Метод узла СИ", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста методом узла ПИ(DCW) в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void PiDCWNodeMethodControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await AddControlAsync(new PiDCWNodeMethodControl(), "Метод узла ПИ(DCW)", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста методом узла ПИ(ACW) в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void PiACWNodeMethodControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await AddControlAsync(new PiACWNodeMethodControl(), "Метод узла ПИ(ACW)", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом СИ в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void CiMethodExecutorControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await AddControlAsync(new CiMethodExecutor(), "Групповой метод СИ", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом ПИ(ACW) в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void PiACWMethodExecutorControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await AddControlAsync(new PiACWMethodExecutorControl(), "Групповой метод ПИ(ACW)", TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом ПИ(DCW) в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void PiDCWMethodExecutorControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await AddControlAsync(new PiDCWMethodExecutorControl(), "Групповой метод ПИ(DCW)", TypeWindow.DeviceControl);
    
    #endregion

    #region Настройки.

    /// <summary>
    /// Добавляет элемент управления для выполнения операции в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Execution_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.Execution.ExecutionControl(), "Выполнение", TypeWindow.Settings);

    /// <summary>
    /// Добавляет элемент управления для настроек конфигурации оборудования в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Config_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.DeviceConfig.DeviceConfigControl(), "Конфигурация оборудования", TypeWindow.Settings);
    //private void Config_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.ConfigSettings.ConfigSettingsControl(), "Конфигурация оборудования");

    /// <summary>
    /// Добавляет элемент управления для настроек погрешностей измерения в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Error_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.MeasurementError.MeasurementErrorControl(), "Погрешности измерений", TypeWindow.Settings);

    /// <summary>
    /// Добавляет элемент управления для управления протоколом в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Protocol_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.ProtocolManager.ProtocolManagerControl(), "Протокол", TypeWindow.Settings);

    #endregion

    #region Администрирование

    /// <summary>
    /// Добавляет элемент управления для работы с флешкой.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void USB_Handler(object sender, MouseButtonEventArgs e)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      KeyManagementWindow keyManagementWindow = new KeyManagementWindow();
      keyManagementWindow.ShowDialog();
      this.Effect = null;
    }

    /// <summary>
    /// Добавляет элемент управления элемент управления для работы с ППУ.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Gpt_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new UI.Controls.GPT.GPTPunchControl(), "GptManagement", TypeWindow.Settings).ConfigureAwait(true);

    /// <summary>
    /// Добавляет элемент управления для отправки команды в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void SendCommand_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.SendCommand.SendCommandControl(), "Send Command", TypeWindow.Settings);

    /// <summary>
    /// Добавляет элемент управления для работы с логами в multiEditors.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Logger_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.LoggerMessage.LoggerControl(), "Logger", TypeWindow.Settings);

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

    /// <summary>
    /// Обрабатывает событие закрытия окна, предотвращая закрытие, если приложение заблокировано, и выполняет необходимые действия по завершению работы системы.
    /// </summary>
    private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (isLocked)
      {
        e.Cancel = true;
        MessageBox.Show("Приложение заблокировано и не может быть закрыто.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        LogWarning("Попытка закрыть приложение, когда оно заблокировано.");
      }
      else
      {
        LogInformation("Закрытие приложения.");

        // TODO : Раскоментить
        // await Core.ManagerShassy.Function.StopPowerAsync(ConfigCollector.GetManagerShassyIp());
        // await Task.Delay(1000);
        // await Core.Communication.CommunicationManager.ResetAllSystem();
      }
    }

    #endregion
  }
}
