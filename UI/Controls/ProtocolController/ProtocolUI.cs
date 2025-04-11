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

namespace UI.Controls.ProtocolController
{
  /// <inheritdoc />
  public partial class ProtocolController : IUserMessageService
  {
    #region Поля.


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

    #endregion

    #region Основные настройки.




    #endregion

    #region Основные методы кнопок.

    #region Начало и конец.

    /// <summary>
    /// Прерывает выполнение текущего процесса.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию прерывания выполнения.</returns>
    public async Task AbortExecution() => await ActionExecutor.StopAsync(DelegateRegistry.StopDelegate);

    /// <summary>
    /// Начинает запуск измерения.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию измерения.</returns>
    private async Task StartAsync() => await ActionExecutor.StartAsync(DelegateRegistry.StartDelegate, DelegateRegistry.StopDelegate, header.Text, DelegateRegistry.IsRepeatEnabled, DelegateRegistry.PreActionDelegate);

    /// <summary>
    /// Завершение текущей выполняемой задачи.Ну т
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию завершения.</returns>
    private async Task StopAsync() => await ActionExecutor.StopAsync(DelegateRegistry.StopDelegate);

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
    public async Task PauseAsync() => await PauseManager.PauseAsync();

    /// <summary>
    /// Возобновляет метод после паузы.
    /// </summary>
    public void Resume() => PauseManager.Resume();

    #endregion

    #region Повтор и зацикливание.

    /// <summary>
    /// Запускает цикл выполнения делегата измерения, отображая кнопки "Остановить" и "Завершить".
    /// </summary>
    private async void LoopMeasureEvent() => await ActionExecutor.LoopMeasureEvent(DelegateRegistry.ReturnDelegate, DelegateRegistry.StopDelegate);

    /// <summary>
    /// Выполняет делегат измерения один раз. Если делегат null, выполняется завершение.
    /// </summary>
    private async void ReturnMeasureEvent() => await ActionExecutor.ReturnMeasureEvent(DelegateRegistry.ReturnDelegate, DelegateRegistry.StopDelegate);

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
    internal async Task<bool> ProcessStepModeAsync(bool stepMode)
    {
      bool result = false;
      await Application.Current.Dispatcher.Invoke(async () => result = await ActionExecutor.ProcessStepModeAsync(stepMode, _mainWindow.CommandBindings, _mainWindow.InputBindings));
      return result;
    }

    /// <summary>
    /// Проверка на пошаговый режим.
    /// </summary>
    public async Task CheckStepModeAsync()
    {
      CommandBindingCollection commandBindings = null;
      InputBindingCollection inputBindings = null;

      // Получаем доступ к свойствам из UI-потока
      await Dispatcher.InvokeAsync(() =>
      {
        commandBindings = this.CommandBindings;
        inputBindings = this.InputBindings;
      });

      await ActionExecutor.CheckStepModeAsync(commandBindings, inputBindings);
    }

    #endregion

    /// <summary>
    /// Возвращает токен отмены для текущего действия.
    /// </summary>
    /// <returns>Токен отмены <see cref="CancellationToken"/>.</returns>
    public CancellationToken GetCancellationToken()
    {
      return ActionExecutor.CancellationTokenSource.Token;
    }

    public Task<bool> ShowMessageAsync(ShowMessageModel model) => this.MessageManager.ShowMessageAsync(model);
  }
}
