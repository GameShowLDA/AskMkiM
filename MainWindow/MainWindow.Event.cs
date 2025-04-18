//using System.Windows;
//using System.Windows.Input;
//using Core.ConfigCollector;
//using Microsoft.Win32;
//using static AppConfig.Config.SystemStateManager;
//using static Utilities.LoggerUtility;
//using Mode.Metrology.KC;
//using Mode.Metrology.IE;
//using Mode.Metrology.CI;
//using Mode.ServicesTest.MKR;
//using Mode.ServicesTest.UKSH;
//using Mode.ServicesTest.MINT;
//using Mode.ServicesTest.MESH;

//namespace MainWindowProgram
//{
//  public partial class MainWindow
//  {

//    #region Кнопки управления главным окном.

//    /// <summary>
//    /// Обработчик нажатия на кнопку "Максимизировать", изменяет состояние окна между нормальным и максимизированным.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void maximizeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
//    {
//      if (this.WindowState != WindowState.Maximized)
//      {
//        this.WindowState = WindowState.Maximized;
//      }
//      else
//      {
//        this.WindowState = WindowState.Normal;
//      }
//    }

//    /// <summary>
//    /// Обработчик нажатия на кнопку "Минимизировать", сворачивает окно.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void minimizeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e) => this.WindowState = WindowState.Minimized;

//    /// <summary>
//    /// Обработчик нажатия на кнопку "Закрыть", завершает работу приложения.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void exitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown();

//    /// <summary>
//    /// Обработчик перетаскивания окна при нажатии и удерживании левой кнопки мыши на главном меню.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void mainMenu_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => this.DragMove();

//    #endregion

//    #region Файл.

//    /// <summary>
//    /// Обработчик нажатия на кнопку "Открыть", открывает диалоговое окно для выбора текстового файла и загружает его в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void Open_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
//    {
//      if (await GetIsLocked())
//      {
//        MessageBox.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
//        LogWarning("Попытка открыть файл, когда приложение заблокировано.");
//      }
//      else
//      {
//        OpenFileDialog openFileDialog = new OpenFileDialog
//        {
//          Title = "Выберите текстовый файл"
//        };

//        if (openFileDialog.ShowDialog() == true)
//        {
//          string filePath = openFileDialog.FileName;
//          multiEditors.OpenFile(filePath);
//          LogInformation($"Файл открыт: {filePath}");
//        }
//      }
//    }

//    /// <summary>
//    /// Обработчик создания нового файла в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void Create_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
//    {
//      if (await GetIsLocked())
//      {
//        MessageBox.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
//        LogWarning("Попытка создать новый файл, когда приложение заблокировано.");
//      }
//      else
//      {
//        multiEditors.CreateNewFile();
//        LogInformation("Создан новый файл.");
//      }
//    }

//    /// <summary>
//    /// Закрывает ПО.
//    /// </summary>
//    /// <param name="sender"></param>
//    /// <param name="e"></param>
//    private void Exit_Handler(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown();

//    #endregion

//    #region Метрология.

//    /// <summary>
//    /// Добавляет пользовательский элемент управления режима КС в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void KC_Handler(object sender, MouseButtonEventArgs e)
//    {
//      Application.Current.Dispatcher.Invoke(async () =>
//      {
//        await AddControlAsync(new KcControl(), "Режим КС");
//      });
//    }

//    /// <summary>
//    /// Добавляет пользовательский элемент управления режима ИЕ в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void IE_Handler(object sender, MouseButtonEventArgs e) => await AddControlAsync(new IeControl(), "Режим ИЕ");

//    /// <summary>
//    /// Добавляет пользовательский элемент управления режима СИ в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void CI_Handler(object sender, MouseButtonEventArgs e) => await AddControlAsync(new CiControl(), "Режим СИ");

//    #endregion

//    #region Сервис

//    /// <summary>
//    /// Обработчик события нажатия, который добавляет в контейнер multiEditors пользовательский элемент управления для МКР350.
//    /// Создает новый экземпляр <see cref="MkrControl"/> и вызывает асинхронный метод <see cref="AddControlAsync"/> с наименованием "Модуль МКР350".
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void Mkr_Handler(object sender, MouseButtonEventArgs e)
//        => await AddControlAsync(new MkrControl(), "Модуль МКР350");

//    /// <summary>
//    /// Обработчик события нажатия, который добавляет в контейнер multiEditors пользовательский элемент управления для модуля УКШ.
//    /// Создает новый экземпляр <see cref="UkshControl"/> и вызывает асинхронный метод <see cref="AddControlAsync"/> с наименованием "Модуль УКШ".
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void Uksh_Handler(object sender, MouseButtonEventArgs e)
//        => await AddControlAsync(new UkshControl(), "Модуль УКШ");

//    /// <summary>
//    /// Обработчик события нажатия, который добавляет в контейнер multiEditors пользовательский элемент управления для модуля МИНТ.
//    /// Создает новый экземпляр <see cref="MintControl"/> и вызывает асинхронный метод <see cref="AddControlAsync"/> с наименованием "Модуль МИНТ".
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void Mint_Handler(object sender, MouseButtonEventArgs e)
//        => await AddControlAsync(new MintControl(), "Модуль МИНТ");

//    /// <summary>
//    /// Обработчик события нажатия, который добавляет в контейнер multiEditors пользовательский элемент управления для модуля МеШ.
//    /// Создает новый экземпляр <see cref="MeshControl"/> и вызывает асинхронный метод <see cref="AddControlAsync"/> с наименованием "Модуль МеШ".
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void Mesh_Handler(object sender, MouseButtonEventArgs e)
//        => await AddControlAsync(new MeshControl(), "Модуль МеШ");

//    #endregion

//    #region Самоконтроль.

//    /// <summary>
//    /// Добавляет элемент управления для самоконтроля одного из модулей в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private async void Test_Self(object sender, MouseButtonEventArgs e) => await AddControlAsync(new Mode.SelfControl.Module.ModuleSelfControl(), "Самоконтроль модулей");

//    #endregion

//    #region Тесты.

//    /// <summary>
//    /// Добавляет элемент управления для теста методом узла в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void NodeMethodControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.TestSuite.NodeMethod.NodeMethodControl(), "Метод узла");

//    private void CrossTestMkrControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.TestSuite.CrossTestMkr.CrossTestMkrControl(), "Перекрёстный тест");

//    #endregion

//    #region Настройки.

//    /// <summary>
//    /// Добавляет элемент управления для выполнения операции в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void Execution_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.Execution.ExecutionControl(), "Выполнение");

//    /// <summary>
//    /// Добавляет элемент управления для настроек конфигурации оборудования в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void Config_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.ConfigSettings.ConfigSettingsControl(), "Конфигурация оборудования");

//    /// <summary>
//    /// Добавляет элемент управления для настроек погрешностей измерения в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void Error_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.MeasurementError.MeasurementErrorControl(), "Погрешности измерений");

//    /// <summary>
//    /// Добавляет элемент управления для управления протоколом в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void Protocol_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.ProtocolManager.ProtocolManagerControl(), "Протокол");

//    #endregion

//    #region Администрирование

//    /// <summary>
//    /// Добавляет элемент управления для работы с флешкой.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void USB_Handler(object sender, MouseButtonEventArgs e)
//    {
//      this.Effect = new System.Windows.Media.Effects.BlurEffect();
//      KeyManagementWindow keyManagementWindow = new KeyManagementWindow();
//      keyManagementWindow.ShowDialog();
//      this.Effect = null;
//    }

//    /// <summary>
//    /// Добавляет элемент управления для отправки команды в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void SendCommand_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.SendCommand.SendCommandControl(), "Send Command");

//    /// <summary>
//    /// Добавляет элемент управления для работы с логами в multiEditors.
//    /// </summary>
//    /// <param name="sender">Объект, вызвавший событие.</param>
//    /// <param name="e">Аргументы события нажатия мыши.</param>
//    private void Logger_Handler(object sender, MouseButtonEventArgs e) => AddControlAsync(new Mode.Settings.LoggerMessage.LoggerControl(), "Logger");

//    #endregion

//    #region Обработка событий.

//    /// <summary>
//    /// Обрабатывает изменения прав администратора, обновляя видимость соответствующих элементов интерфейса.
//    /// </summary>
//    private void ApplicationDataHandler_AdminRightsChanged(bool newValue)
//    {
//      Application.Current.Dispatcher.Invoke(() =>
//      {
//        if (newValue)
//        {
//          this.Admin.Visibility = Visibility.Visible;
//          LogInformation("Права администратора предоставлены.");
//        }
//        else
//        {
//          this.Admin.Visibility = Visibility.Collapsed;
//          LogInformation("Права администратора отозваны.");
//        }
//      });
//    }

//    /// <summary>
//    /// Обрабатывает изменения состояния блокировки приложения, изменяя видимость панели и логируя изменения.
//    /// </summary>
//    private void ApplicationDataHandler_LockedChanged(bool newValue)
//    {
//      Application.Current.Dispatcher.Invoke(() =>
//      {
//        if (newValue)
//        {
//          TopPanel.Visibility = Visibility.Collapsed;
//          isLocked = true;
//          LogInformation("Приложение заблокировано.");
//        }
//        else
//        {
//          TopPanel.Visibility = Visibility.Visible;
//          isLocked = false;
//          LogInformation("Приложение разблокировано.");
//        }
//      });
//    }

//    /// <summary>
//    /// Обрабатывает событие закрытия окна, предотвращая закрытие, если приложение заблокировано, и выполняет необходимые действия по завершению работы системы.
//    /// </summary>
//    private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
//    {
//      if (isLocked)
//      {
//        e.Cancel = true;
//        MessageBox.Show("Приложение заблокировано и не может быть закрыто.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
//        LogWarning("Попытка закрыть приложение, когда оно заблокировано.");
//      }
//      else
//      {
//        LogInformation("Закрытие приложения.");
//        await Core.Communication.CommunicationManager.ResetAllSystem();
//        await Task.Delay(500);
//        await Core.ManagerShassy.Function.StopPowerAsync(ConfigCollector.GetManagerShassyIp());
//      }
//    }

//    #endregion
//  }
//}
