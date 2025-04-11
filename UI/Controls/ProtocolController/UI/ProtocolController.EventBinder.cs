using System.Windows;
using System.Windows.Input;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolController
{
  /// <summary>
  /// Содержит методы для привязки обработчиков событий к кнопкам интерфейса ProtocolController.
  /// </summary>
  public partial class ProtocolController
  {
    /// <summary>
    /// Делегат, создающий структуру для отработки нажатий по кнопкам.
    /// </summary>
    /// <param name="sender">Экземпляр кнопки.</param>
    /// <param name="e">Событие кнопки.</param>
    public delegate void PreviewMouseDownEventHandler(object sender, MouseButtonEventArgs e);

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Запустить".
    /// </summary>
    public event PreviewMouseDownEventHandler StartMeasureResistanceButtonPreviewMouseDown;

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Повторить".
    /// </summary>
    public event PreviewMouseDownEventHandler ReturnMeasureResistanceButtonPreviewMouseDown;

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Зациклить".
    /// </summary>
    public event PreviewMouseDownEventHandler LoopMeasureResistanceButtonPreviewMouseDown;

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Остановить".
    /// </summary>
    public event PreviewMouseDownEventHandler PauseButtonPreviewMouseDown;

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Поверх(F10)".
    /// </summary>
    public event PreviewMouseDownEventHandler TopLayerButtonPreviewMouseDown;

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Вглубь(F11)".
    /// </summary>
    public event PreviewMouseDownEventHandler BottomLayerButtonPreviewMouseDown;

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Продолжить".
    /// </summary>
    public event PreviewMouseDownEventHandler NextButtonPreviewMouseDown;

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Завершить".
    /// </summary>
    public event PreviewMouseDownEventHandler ExitButtonPreviewMouseDown;

    /// <summary>
    /// Настраивает обработчики событий для всех кнопок интерфейса.
    /// Вызывается один раз при инициализации контрола.
    /// </summary>
    private void SetupButtons()
    {
      SetupEventHandlers();
      ShowOnlyStartButton();
    }

    /// <summary>
    /// Привязывает обработчики событий к кнопкам управления.
    /// </summary>
    private void SetupEventHandlers()
    {
      SetEventControls();
      startButton.PreviewMouseDown += StartMeasureResistanceButton_PreviewMouseDown;
      returnButton.PreviewMouseDown += ReturnMeasureResistanceButton_PreviewMouseDown;
      loopButton.PreviewMouseDown += LoopMeasureResistanceButton_PreviewMouseDown;
      pauseButton.PreviewMouseDown += StopButton_PreviewMouseDown;
      stepOverButton.PreviewMouseDown += TopLayer_PreviewMouseDown;
      stepIntoButton.PreviewMouseDown += BottomLayer_PreviewMouseDown;
      continueButton.PreviewMouseDown += NextButton_PreviewMouseDown;
      exitButton.PreviewMouseDown += ExitButton_PreviewMouseDown;
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

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки "Запустить".
    /// </summary>
    private void StartMeasureResistanceButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation("Сработан обработчик события для кнопки \"Запустить\"");
      SetNonVisibleAllButton();
      ShowOnlyStopAndFinishButtons(ActionExecutor.StepMode);
      StartMeasureResistanceButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки "Остановить".
    /// </summary>
    private void StopButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation("Сработан обработчик события для кнопки \"Остановить\"");
      SetNonVisibleAllButton();
      continueButton.Visibility = Visibility.Visible;
      exitButton.Visibility = Visibility.Visible;
      PauseButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки "Продолжить".
    /// </summary>
    private void NextButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation("Сработан обработчик события для кнопки \"Продолжить\"");
      SetNonVisibleAllButton();
      pauseButton.Visibility = Visibility.Visible;
      exitButton.Visibility = Visibility.Visible;
      NextButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки "Завершить".
    /// </summary>
    private void ExitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation("Сработан обработчик события для кнопки \"Завершить\"");
      SetNonVisibleAllButton();
      startButton.Visibility = Visibility.Visible;
      ExitButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки "Повторить".
    /// </summary>
    private void ReturnMeasureResistanceButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation("Сработан обработчик события для кнопки \"Повторить\"");
      ReturnMeasureResistanceButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки "Зациклить".
    /// </summary>
    private void LoopMeasureResistanceButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation("Сработан обработчик события для кнопки \"Зациклить\"");
      LoopMeasureResistanceButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки "Поверх".
    /// </summary>
    private void TopLayer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation("Сработан обработчик события для кнопки \"Поверх\"");
      TopLayerButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки "Вглубь".
    /// </summary>
    private void BottomLayer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation("Сработан обработчик события для кнопки \"Вглубь\"");
      BottomLayerButtonPreviewMouseDown?.Invoke(this, e);
    }
  }
}
