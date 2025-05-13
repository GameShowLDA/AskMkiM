using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using AppConfiguration.Base;
using AppConfiguration.Interface;
using NewCore.Base.Device;
using Utilities;
using Utilities.Models;
using static AppConfiguration.Protocol.ProtocolConfig;
using static Utilities.DelegateManager;
using static Utilities.Models.ShowMessageModel;
using static AppConfiguration.SystemState.SystemStateManager;

namespace UI.Controls.ProtocolNew
{
  /// <inheritdoc />
  public partial class ProtocolUI : IUserMessageService
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

    #endregion

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
    public void SetSettings(
      UIElement MainWindow,
      StartDelegate StartDelegate,
      bool isRepeatEnabled,
      StopDelegate StopDelegate = null,
      ReturnDelegate ReturnDelegate = null,
      PreActionDelegate preActionDelegate = null
      )
    {
      try
      {
        _mainWindow = MainWindow;
        _stopDelegate = StopDelegate;
        _startDelegate = StartDelegate;
        _returnDelegate = ReturnDelegate;
        _preActionDelegate = preActionDelegate;

        if (ReturnDelegate != null)
        {
          _isRepeatEnabled = true;
        }
      }
      catch (Exception ex)
      {
        LoggerUtility.LogException("Ошибка загрузки элемента", ex);
        throw;
      }
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
    /// Выводит информацию в протокол.
    /// </summary>
    /// <param name="showMessageModel">Модель сообщения.</param>
    /// <returns>Возвращает режим по шагам.</returns>
    public async Task ShowMessageAsync(ShowMessageModel showMessageModel, bool IsBlockStart = false, bool SkipStepModeCheck = false)
    {

      if (IsBlockStart)
      {
        StepControlManager.ExitBlock();
        StepControlManager.EnterBlock();
      }

      if (await GetTimeStart())
      {
        showMessageModel.Time = _stopwatch.Elapsed.ToString(@"mm\:ss\.fff", System.Globalization.CultureInfo.InvariantCulture);
      }

      if (!await GetShowDetailedProtocol())
      {
        if (LastModelMeassage != null && LastModelMeassage.CanBeDeleted && !LastModelMeassage.ExecutionError)
        {
          await protocolTextBox.RemoveLastLinesAsync();
        }

        LastModelMeassage = showMessageModel;
      }

      await protocolTextBox.AppendLineAsync(showMessageModel);

      if (ActionExecutor.IsPaused)
      {
        await ActionExecutor.WaitWhilePausedAsync(this);
      }

      if (StepControlManager.StepMode && !SkipStepModeCheck)
      {
        if (!StepControlManager.IsStepInto && StepControlManager.InsideBlock)
        {
          // Поверх — внутри блока, пропускаем ожидание
        }
        else
        {
          await KeyboardManager.WaitForNextStepKeyAsync();
        }
      }

      await Task.Delay(1);
    }

    /// <summary>
    /// Полностью очищает протокол и сбрасывает последнее сообщение.
    /// </summary>
    /// <returns>Возвращает признак успешного завершения операции.</returns>
    public async Task<bool> ClearAllMessagesAsync()
    {
      await protocolTextBox.ClearAsync();
      LastModelMeassage = null;

      if (ActionExecutor.IsPaused)
      {
        await ActionExecutor.WaitWhilePausedAsync(this);
      }

      return ActionExecutor.StepMode;
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
      string filename = $"KC_{dateTime}.txt";
      string fullPath = Path.Combine(FileLocations.DataSaveDirectory, filename);

      var lines = protocolTextBox.Messages.Select(m =>
          $"{m.Header}: {m.Message}");

      await File.WriteAllLinesAsync(fullPath, lines);
    }

    /// <summary>
    /// Выводит протокол на печать.
    /// </summary>
    public void PrintProtocol(IEnumerable<ShowMessageModel> messages)
    {
      PrintDialog printDialog = new PrintDialog();
      if (printDialog.ShowDialog() != true)
        return;

      FlowDocument document = new FlowDocument
      {
        PagePadding = new Thickness(50),
        ColumnWidth = double.PositiveInfinity
      };

      foreach (var model in messages)
      {
        var paragraph = new Paragraph();

        if (!string.IsNullOrWhiteSpace(model.Header))
        {
          paragraph.Inlines.Add(new Run(model.Header)
          {
            Foreground = new SolidColorBrush(model.HeaderColor ?? Colors.Black),
            FontSize = 18,
            FontWeight = FontWeights.Bold
          });
        }

        if (!string.IsNullOrWhiteSpace(model.Message))
        {
          paragraph.Inlines.Add(new Run(": "));

          paragraph.Inlines.Add(new Run(model.Message)
          {
            Foreground = new SolidColorBrush(model.MessageColor ?? Colors.Black),
            FontSize = 18
          });
        }

        document.Blocks.Add(paragraph);
      }

      IDocumentPaginatorSource source = document;
      printDialog.PrintDocument(source.DocumentPaginator, "Печать протокола...");
    }

    #endregion

    /// <summary>
    /// Возвращает токен отмены для текущего действия.
    /// </summary>
    /// <returns>Токен отмены <see cref="CancellationToken"/>.</returns>
    public CancellationToken GetCancellationToken() =>
      ActionExecutor.CancellationTokenSource?.Token ?? CancellationToken.None;

  }
}
