using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using Utilities.Models;

namespace UI.Controls.ProtocolController
{
  /// <summary>
  /// Логика взаимодействия для Protocol.xaml
  /// </summary>
  public partial class Protocol : UserControl
  {
    public AvalonTextOutput Message { get; set; }
    public ProtocolButtonManager ButtonManager { get; set; }

    public ProtocolExecutionManager ExecutionManager { get; set; }
    public ProtocolExecutionRunner protocolExecutionRunner { get; set; }

    internal ProtocolExecutionSettings ProtocolExecutionSettings;

    public StepExecutionManager StepManager { get; set; }
    public Protocol()
    {
      InitializeComponent();
      Message = new AvalonTextOutput(textEditor, this);
      ButtonManager = new ProtocolButtonManager(this);
      ButtonManager.ShowOnlyStartButton();
      ExecutionManager = new ProtocolExecutionManager();
      protocolExecutionRunner = new ProtocolExecutionRunner(this);
      startButton.PreviewMouseDown += StartButton_PreviewMouseDown;
      exitButton.PreviewMouseDown += ExitButton_PreviewMouseDown;
      pauseButton.PreviewMouseDown += PauseButton_PreviewMouseDown;
      continueButton.PreviewMouseDown += ContinueButton_PreviewMouseDown;

      StepManager = new StepExecutionManager();
      StepManager.SetMode(StepExecutionMode.StepInto);

      stepIntoButton.Click += (_, __) => StepManager.StepInto();
      stepOverButton.Click += (_, __) => StepManager.StepOverBlock();
    }


    private async void ExitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await protocolExecutionRunner.FinalizeAsync(
          ProtocolExecutionSettings?.StopDelegate,
          "Процесс завершения"
          );
    }

    private async void StartButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (ProtocolExecutionSettings == null)
      {
        await Message.AppendLineAsync(new ShowMessageModel(
          "Ошибка: настройки выполнения не заданы.",
          ShowMessageModel.ErrorMessage.TitleColor));
        return;
      }

      await protocolExecutionRunner.StartAsync(ProtocolExecutionSettings, "Процесс");
    }

    /// <summary>
    /// Устанавливает основные настройки выполнения действий.
    /// </summary>
    public void SetSettings(ProtocolExecutionSettings protocolExecutionSettings)
    {
      ProtocolExecutionSettings = protocolExecutionSettings;
      ExecutionManager.SetSettings(protocolExecutionSettings);
    }

    private void PauseButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (protocolExecutionRunner.IsRunning)
      {
        protocolExecutionRunner.PauseManager?.PauseAsync(this);
        ButtonManager.ShowButtonsOnPause();
      }
    }

    private void ContinueButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (protocolExecutionRunner.IsRunning)
      {
        protocolExecutionRunner.PauseManager?.Resume();
        ButtonManager.ShowOnlyStopAndFinishButtons();
      }
    }


    #region Элементы управления

    /// <summary>
    /// Возвращает кнопку "Запуск".
    /// </summary>
    public Button StartButton => startButton;

    /// <summary>
    /// Возвращает кнопку "Повторить".
    /// </summary>
    public Button RepeatButton => returnButton;

    /// <summary>
    /// Возвращает кнопку "Зациклить".
    /// </summary>
    public Button LoopButton => loopButton;

    /// <summary>
    /// Возвращает кнопку "Остановить".
    /// </summary>
    public Button PauseButton => pauseButton;

    /// <summary>
    /// Возвращает кнопку "Поверх".
    /// </summary>
    public Button StepOverButton => stepOverButton;

    /// <summary>
    /// Возвращает кнопку "Вглубь".
    /// </summary>
    public Button StepIntoButton => stepIntoButton;

    /// <summary>
    /// Возвращает кнопку "Продолжить".
    /// </summary>
    public Button ContinueButton => continueButton;

    /// <summary>
    /// Возвращает кнопку "Завершить".
    /// </summary>
    public Button ExitButton => exitButton;

    /// <summary>
    /// Возвращает текстовый элемент.
    /// </summary>
    public ICSharpCode.AvalonEdit.TextEditor TextEditor => textEditor;
    #endregion
  }
}
