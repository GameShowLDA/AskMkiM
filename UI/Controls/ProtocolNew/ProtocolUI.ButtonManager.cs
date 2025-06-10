using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolNew
{
  partial class ProtocolUI
  {
    private TaskCompletionSource<bool>? _adminButtonTcs;

    #region Кнопки.

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

    #endregion

    #region Делегаты по нажатию кнопок.

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

    #endregion

    #region Свойства, связанное с отображением кнопок.

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Запустить".
    /// </summary>
    public Visibility StartMeasureResistanceButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => startButton.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => startButton.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Повторить".
    /// </summary>
    public Visibility ReturnMeasureResistanceButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => returnButton.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => returnButton.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Зациклить".
    /// </summary>
    public Visibility LoopMeasureResistanceButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => loopButton.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => loopButton.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Остановить".
    /// </summary>
    public Visibility PauseButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => pauseButton.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => pauseButton.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Поверх".
    /// </summary>
    public Visibility StepOverButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => stepOverButton.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => stepOverButton.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Вглубь".
    /// </summary>
    public Visibility StepIntoButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => stepIntoButton.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => stepIntoButton.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Продолжить".
    /// </summary>
    public Visibility NextButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => continueButton.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => continueButton.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Завершить".
    /// </summary>
    public Visibility ExitButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => exitButton.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => exitButton.Visibility = value); }
    }

    #endregion

    #region События кнопок.

    #region События основных кнопок.

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки StartMeasureResistanceButton.
    /// </summary>
    private void StartMeasureResistanceButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Запустить\"");
      SetNonVisibleAllButton();
      ShowOnlyStopAndFinishButtons(ActionExecutor.StepMode);
      StartMeasureResistanceButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки StopButton.
    /// </summary>
    private void StopButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Остановить\"");

      SetNonVisibleAllButton();
      continueButton.Visibility = Visibility.Visible;
      exitButton.Visibility = Visibility.Visible;

      PauseButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки NextButton.
    /// </summary>
    private void NextButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Продолжить\"");

      SetNonVisibleAllButton();

      pauseButton.Visibility = Visibility.Visible;
      exitButton.Visibility = Visibility.Visible;

      NextButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки ExitButton.
    /// </summary>
    private void ExitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Завершить\"");

      SetNonVisibleAllButton();
      startButton.Visibility = Visibility.Visible;

      ExitButtonPreviewMouseDown?.Invoke(this, e);
    }
    #endregion

    #region События дополнительных кнопок.

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки ReturnMeasureResistanceButton.
    /// </summary>
    private void ReturnMeasureResistanceButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Повторить\"");
      ReturnMeasureResistanceButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки LoopMeasureResistanceButton.
    /// </summary>
    private void LoopMeasureResistanceButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Зациклить\"");
      LoopMeasureResistanceButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки TopLayer.
    /// </summary>
    private void TopLayer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Поверх\"");
      TopLayerButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки BottomLayer.
    /// </summary>
    private void BottomLayer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Вглубь\"");
      BottomLayerButtonPreviewMouseDown?.Invoke(this, e);
    }
    #endregion

    #endregion

    #region Методы.

    /// <summary>
    /// Настраивает обработчики событий для кнопок управления и элементов компонента.
    /// </summary>
    private void SetupButtons()
    {
      SetupEventHandlers();
      ShowOnlyStartButton();
    }

    /// <summary>
    /// Настраивает обработчики событий для кнопок управления и элементов компонента.
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
    /// Скрывает все кнопки управления.
    /// </summary>
    private void SetNonVisibleAllButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        startButton.Visibility = Visibility.Collapsed;
        pauseButton.Visibility = Visibility.Collapsed;
        continueButton.Visibility = Visibility.Collapsed;
        exitButton.Visibility = Visibility.Collapsed;

        returnButton.Visibility = Visibility.Collapsed;
        loopButton.Visibility = Visibility.Collapsed;

        stepOverButton.Visibility = Visibility.Collapsed;
        stepIntoButton.Visibility = Visibility.Collapsed;

        adminContinue.Visibility = Visibility.Collapsed;
        adminExit.Visibility = Visibility.Collapsed;
      });
    }

    /// <summary>
    /// Отображает только кнопку "Старт", скрывая все остальные кнопки.
    /// </summary>
    public void ShowOnlyStartButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();
        startButton.Visibility = Visibility.Visible;
      });
    }

    /// <summary>
    /// Отображает кнопки при выполнении.
    /// </summary>
    /// <param name="stepMode">Режим по шагам.</param>
    public void ShowOnlyStopAndFinishButtons()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();
        if (ActionExecutor.StepMode)
        {
          StepOverButton.Visibility = Visibility.Visible;
          StepIntoButton.Visibility = Visibility.Visible;
        }

        pauseButton.Visibility = Visibility.Visible;
        exitButton.Visibility = Visibility.Visible;
      });
    }

    /// <summary>
    /// Отображает кнопки при выполнении.
    /// </summary>
    /// <param name="stepMode">Режим по шагам.</param>
    public void ShowOnlyStopAndFinishButtons(bool stepMode)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();
        if (stepMode)
        {
          StepOverButton.Visibility = Visibility.Visible;
          StepIntoButton.Visibility = Visibility.Visible;
        }

        pauseButton.Visibility = Visibility.Visible;
        exitButton.Visibility = Visibility.Visible;
      });
    }

    /// <summary>
    /// Скрывает кнопки режима по шагам.
    /// </summary>
    public void SetNotVisibleStepButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        StepIntoButton.Visibility = Visibility.Collapsed;
        StepOverButton.Visibility = Visibility.Collapsed;
      });
    }

    /// <summary>
    /// Отображает кнопки при паузе.
    /// </summary>
    public void ShowButtonsOnPause()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();
        NextButtonVisibility = Visibility.Visible;
        ExitButtonVisibility = Visibility.Visible;
      });
    }

    /// <summary>
    /// Отображает кнопки при зациклить и повторить.
    /// </summary>
    public void ShowAdditionalFunctionButtons()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();
        LoopMeasureResistanceButtonVisibility = Visibility.Visible;
        ReturnMeasureResistanceButtonVisibility = Visibility.Visible;
        ExitButtonVisibility = Visibility.Visible;
      });
    }

    public void SetupAdminButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();

        adminExit.Visibility = Visibility.Visible;
        adminContinue.Visibility = Visibility.Visible;
      });
    }

    #endregion

   
  }
}
