using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Core.Model;
using Utilities.Models;
using static AppConfig.Config.ProtocolConfig;
using static Utilities.DelegateManager;
using static Utilities.Models.ShowMessageModel;

namespace UI.Controls.Protocol
{
  /// <inheritdoc />
  public partial class ProtocolUI
  {
    #region Поля.

    /// <summary>
    /// Последнее отображенное сообщение в протоколе.
    /// </summary>
    ShowMessageModel LastModelMeassage;

    /// <summary>
    /// Возвращает текущий статус пошагового режима.
    /// </summary>
    public bool StepMode => ActionExecutor.StepMode;

    /// <summary>
    /// Главное окно приложения, используемое для отображения интерфейса.
    /// </summary>
    UIElement _mainWindow;

    /// <summary>
    /// Экземпляр <see cref="ActionExecutor"/>, используемый для выполнения действий.
    /// </summary>
    readonly ActionExecutor ActionExecutor;

    #region Делегаты выполнения.

    /// <summary>
    /// Делегат, вызываемый для начала действия.
    /// </summary>
    StartDelegate _startDelegate;

    /// <summary>
    /// Делегат, вызываемый для остановки действия.
    /// </summary>
    StopDelegate _stopDelegate;

    /// <summary>
    /// Делегат, вызываемый для возврата к предыдущему состоянию.
    /// </summary>
    ReturnDelegate _returnDelegate;

    PreActionDelegate _preActionDelegate;

    bool _isRepeatEnabled;
    #endregion

    #region Для работа с текстовым редактором.

    /// <summary>
    /// Сообщение и цвет, используемые для отображения успешного выполнения операции.
    /// </summary>
    readonly Tuple<string, Color> goodText = SuccessMessage;

    /// <summary>
    /// Сообщение и цвет, используемые для отображения ошибки выполнения операции.
    /// </summary>
    readonly Tuple<string, Color> errorText = ErrorMessage;

    #endregion

    #region Работа с оборудованием?

    /// <summary>
    /// Список моделей устройств, используемых в действиях.
    /// </summary>
    List<DeviceModel> DeviceModels;

    #endregion

    #endregion

    /// <summary>
    /// Возвращает токен отмены для текущего действия.
    /// </summary>
    /// <returns>Токен отмены <see cref="CancellationToken"/>.</returns>
    public CancellationToken GetCancellationToken()
    {
      return ActionExecutor.CancellationTokenSource.Token;
    }

    #region Основные настройки.

    /// <summary>
    /// Устанавливает основные настройки выполнения действий.
    /// </summary>
    /// <param name="MainWindow">Главное окно приложения.</param>
    /// <param name="StartDelegate">Делегат запуска.</param>
    /// <param name="isRepeatEnabled">Флаг разрешения повторного выполнения.</param>
    /// <param name="StopDelegate">Делегат остановки (необязательно).</param>
    /// <param name="ReturnDelegate">Делегат возврата к предыдущему состоянию (необязательно).</param>
    /// <param name="preActionDelegate">Делегат предварительных действий перед запуском (необязательно).</param>
    public void SetSettings(UIElement MainWindow, StartDelegate StartDelegate, bool isRepeatEnabled, StopDelegate StopDelegate = null, ReturnDelegate ReturnDelegate = null, PreActionDelegate preActionDelegate = null)
    {
      _mainWindow = MainWindow;
      _stopDelegate = StopDelegate;
      _startDelegate = StartDelegate;
      _returnDelegate = ReturnDelegate;
      _preActionDelegate = preActionDelegate;
    }

    /// <summary>
    /// Устанавливает список моделей устройств для использования в действиях.
    /// </summary>
    /// <param name="deviceModels">Список моделей устройств <see cref="DeviceModel"/>.</param>
    public void SetDevices(List<DeviceModel> deviceModels)
    {
      DeviceModels = deviceModels;
    }

    /// <summary>
    /// Настраивает события для элементов управления.
    /// </summary>
    public void SetEventControls()
    {
      StartMeasureResistanceButtonPreviewMouseDown += async (sender, e) => await StartAsync();
      PauseButtonPreviewMouseDown += async (sender, e) => await PauseAsync();

      TopLayerButtonPreviewMouseDown += StepAround_PreviewMouseDown;
      BottomLayerButtonPreviewMouseDown += StepIn_PreviewMouseDown;

      NextButtonPreviewMouseDown += (sender, e) => Resume();
      ExitButtonPreviewMouseDown += async (sender, e) => await StopAsync();

      LoopMeasureResistanceButtonPreviewMouseDown += (sender, e) => LoopMeasureEvent();
      ReturnMeasureResistanceButtonPreviewMouseDown += (sender, e) => ReturnMeasureEvent();
    }

    #endregion

    #region Основные методы кнопок.

    #region Начало и конец.

    /// <summary>
    /// Прерывает выполнение текущего процесса.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию прерывания выполнения.</returns>
    public async Task AbortExecution() => await ActionExecutor.StopAsync(_stopDelegate);

    /// <summary>
    /// Начинает запуск измерения.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию измерения.</returns>
    private async Task StartAsync() => await ActionExecutor.StartAsync(_startDelegate, _stopDelegate, header.Text, _isRepeatEnabled, _preActionDelegate);

    /// <summary>
    /// Завершение текущей выполняемой задачи.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    private async Task StopAsync() => await ActionExecutor.StopAsync(_stopDelegate);

    /// <summary>
    /// Выполняет завершающие действия после завершения процесса.
    /// </summary>
    /// <param name="stopDelegate">Делегат завершения процесса (необязательно).</param>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    public async Task FinalizeAsync(StopDelegate stopDelegate = null) => await ActionExecutor.FinalizeAsync(stopDelegate);

    #endregion

    #region Пауза и продолжить.

    /// <summary>
    /// Приостанавливает метод на паузу.
    /// </summary>
    /// <returns></returns>
    public async Task PauseAsync() => await ActionExecutor.PauseAsync();

    /// <summary>
    /// Возобновляет метод после паузы.
    /// </summary>
    public void Resume() => ActionExecutor.Resume(ActionExecutor.StepMode);

    #endregion

    #region Повтор и зацикливание.

    /// <summary>
    /// Запускает цикл выполнения делегата измерения, отображая кнопки "Остановить" и "Завершить".
    /// </summary>
    private async void LoopMeasureEvent() => await ActionExecutor.LoopMeasureEvent(_returnDelegate, _stopDelegate);

    /// <summary>
    /// Выполняет делегат измерения один раз. Если делегат null, выполняется завершение.
    /// </summary>
    private async void ReturnMeasureEvent() => await ActionExecutor.ReturnMeasureEvent(_returnDelegate, _stopDelegate);

    #endregion

    #region По шагам.

    /// <summary>
    /// Обработчик события нажатия на кнопку "Поверх".
    /// </summary>
    private void StepAround_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ActionExecutor.StepAround_PreviewMouseDown(sender, e);

    /// <summary>
    /// Обработчик события нажатия на кнопку "Вглубь".
    /// </summary>
    private void StepIn_PreviewMouseDown(object sender, MouseButtonEventArgs e) => ActionExecutor.StepIn_PreviewMouseDown(sender, e);

    #endregion

    #endregion

    #region Методы.

    /// <summary>
    /// Обрабатывает пошаговый режим.
    /// </summary>
    private async Task<bool> ProcessStepModeAsync(bool stepMode)
    {
      bool result = false;
      await Application.Current.Dispatcher.Invoke(async () => result = await ActionExecutor.ProcessStepModeAsync(stepMode, _mainWindow.CommandBindings, _mainWindow.InputBindings));
      return result;
    }

    /// <summary>
    /// Проверка на пошаговый режим.
    /// </summary>
    public async Task CheckStepModeAsync() => await ActionExecutor.CheckStepModeAsync(this.CommandBindings, this.InputBindings);

    /// <summary>
    /// Выводит информацию в протокол.
    /// </summary>
    /// <param name="showMessageModel">Модель сообщения.</param>
    /// <returns>Возвращает режим по шагам.</returns>
    public async Task<bool> ShowMessageAsync(ShowMessageModel showMessageModel)
    {
      if (!await GetShowDetailedProtocol())
      {
        if (LastModelMeassage != null && LastModelMeassage.CanBeDeleted && !LastModelMeassage.ExecutionError)
        {
          await protocolTextBox.RemoveLastLinesAsync();
        }

        LastModelMeassage = showMessageModel;
      }

      await protocolTextBox.AppendLineAsync(showMessageModel.Header, showMessageModel.Message, showMessageModel.HeaderColor, showMessageModel.MessageColor);

      if (ActionExecutor.IsPaused)
      {
        await ActionExecutor.WaitWhilePausedAsync(this);
      }

      return await ProcessStepModeAsync(ActionExecutor.StepMode);
    }

    /// <summary>
    /// Асинхронно удаляет блок, содержащий указанную строку, из RichTextBox.
    /// </summary>
    /// <param name="textToRemove">Строка для поиска и удаления.</param>
    /// <returns>True, если блок был найден и удален; иначе False.</returns>
    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove) => await protocolTextBox.RemoveLineContainingTextAsync(textToRemove);

    /// <summary>
    /// Сохраняет протокол в файл с автоматически сгенерированным именем в фоновом режиме асинхронно.
    /// </summary>
    public async Task SaveProtocolAsync()
    {
      string dateTime = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss", CultureInfo.CurrentCulture);
      string filename = $"KC_{dateTime}.rtf";
      TextRange range = new TextRange(ProtocolTextBox.Document.ContentStart, ProtocolTextBox.Document.ContentEnd);

      await Task.Run(async () =>
      {
        using (FileStream fileStream = new FileStream($"{AppConfig.FileLocations.DataSaveDirectory}\\{filename}", FileMode.Create))
        {
          await Task.Run(() => range.Save(fileStream, DataFormats.Rtf)).ConfigureAwait(true);
        }
      }).ConfigureAwait(true);
    }

    /// <summary>
    /// Выводит протокол на печать.
    /// </summary>
    public void PrintProtocol()
    {
      PrintDialog printDialog = new PrintDialog();
      if (printDialog.ShowDialog() == true)
      {
        FlowDocument document = new FlowDocument();
        document.PagePadding = new Thickness(50);

        TextRange range = new TextRange(ProtocolTextBox.Document.ContentStart, ProtocolTextBox.Document.ContentEnd);
        using (MemoryStream stream = new MemoryStream())
        {
          range.Save(stream, DataFormats.Xaml);
          stream.Position = 0;

          TextRange documentRange = new TextRange(document.ContentStart, document.ContentEnd);
          documentRange.Load(stream, DataFormats.Xaml);
        }

        IDocumentPaginatorSource source = document;
        printDialog.PrintDocument(source.DocumentPaginator, "Печать протокола...");
      }
    }

    #endregion

    /// <summary>
    /// Пытается подключиться к указанным устройствам и возвращает результат попытки подключения.
    /// </summary>
    /// <param name="deviceModels">Список моделей устройств для подключения.</param>
    /// <param name="messageDelegate">Делегат для обработки сообщений о состоянии подключения.</param>
    /// <returns>True, если все устройства успешно подключены, иначе False.</returns>
    public async Task<bool> AttemptDeviceConnection(List<DeviceModel> deviceModels, MessageDelegate messageDelegate) => await ActionExecutor.AttemptDeviceConnection(deviceModels, messageDelegate);
  }
}
