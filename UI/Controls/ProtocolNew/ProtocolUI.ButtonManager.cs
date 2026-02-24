using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;
using System.Windows;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace UI.Controls.ProtocolNew
{
  public partial class ProtocolUI : IButtonService
  {
    private TaskCompletionSource<bool>? _adminButtonTcs;

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
      get { return Application.Current.Dispatcher.Invoke(() => StartButtonElement.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => StartButtonElement.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Повторить".
    /// </summary>
    public Visibility ReturnMeasureResistanceButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => RepeatButtonElement.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => RepeatButtonElement.Visibility = value); }
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
      get
      {
        return Application.Current.Dispatcher.Invoke(() => PauseButtonElement.Visibility);
      }
      set
      {
        Application.Current.Dispatcher.Invoke(() => PauseButtonElement.Visibility = value);
      }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Поверх".
    /// </summary>
    public Visibility StepOverButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => StepOverButtonElement.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => StepOverButtonElement.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Вглубь".
    /// </summary>
    public Visibility StepIntoButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => StepIntoButtonElement.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => StepIntoButtonElement.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Продолжить".
    /// </summary>
    public Visibility NextButtonVisibility
    {
      get { return Application.Current.Dispatcher.Invoke(() => ContinueButtonElement.Visibility); }
      set { Application.Current.Dispatcher.Invoke(() => ContinueButtonElement.Visibility = value); }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Завершить".
    /// </summary>
    public Visibility ExitButtonVisibility
    {
      get
      {
        return Application.Current.Dispatcher.Invoke(() => StopButtonElement.Visibility);
      }
      set
      {
        Application.Current.Dispatcher.Invoke(() => StopButtonElement.Visibility = value);
      }
    }

    /// <summary>
    /// Получает или устанавливает видимость кнопки "Завершить".
    /// </summary>
    public Visibility ButtonPanelsVisibility
    {
      get
      {
        return Application.Current.Dispatcher.Invoke(() => ButtonPanels.Visibility);
      }
      set
      {
        Application.Current.Dispatcher.Invoke(() => ButtonPanels.Visibility = value);
      }
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

      ShowButtonsOnPause();

      PauseButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки NextButton.
    /// </summary>
    private void NextButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Продолжить\"");

      // Для брейкпоинта "Продолжить" должно отправлять управляющее событие выполнения.
      if (StepControlManager.IsBreakpointStepModeActive)
      {
        StepControlManager.DisableStepMode();
        ShowOnlyStopAndFinishButtons(false);
        ExecutionEventAdapter.ExecutionControlEventAdapter.Raise(ExecutionControlButton.Run);
        return;
      }

      // "Продолжить" в UI всегда продолжает без пошагового режима.
      if (ActionExecutor.StepMode || StepControlManager.StepMode)
      {
        ExecutionConfig.SetStepByStepMode(false);
        StepControlManager.DisableStepMode();
        KeyboardManager.TriggerStep();
      }

      ShowOnlyStopAndFinishButtons(false);

      NextButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки ExitButton.
    /// </summary>
    private void ExitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Завершить\"");

      SetNonVisibleAllButton();
      StartButtonElement.Visibility = Visibility.Visible;
      ExitButtonPreviewMouseDown?.Invoke(this, e);
    }
    private void RegisterHotkeys()
    {
      KeyboardManager.OnStartPressed = () =>
        Application.Current.Dispatcher.Invoke(() =>
        {
          ExecutionConfig.SetStepByStepMode(false);
          StartMeasureResistanceButton_PreviewMouseDown(StartButtonElement, CreateMouseArgs());
        });

      KeyboardManager.OnStartPressedByStepMode = () =>
        Application.Current.Dispatcher.Invoke(() =>
        {
          ExecutionConfig.SetStepByStepMode(true);
          StartMeasureResistanceButton_PreviewMouseDown(StartButtonElement, CreateMouseArgs());
        });


      KeyboardManager.OnExitPressed = () =>
        Application.Current.Dispatcher.Invoke(() =>
        {
          if (Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime.DrawerHostService.Instance.ShouldBlockGlobalInput)
          {
            return;
          }

          ExitButton_PreviewMouseDown(StopButtonElement, CreateMouseArgs());
        });

      KeyboardManager.OnPausePressed = () =>
      {
        Application.Current.Dispatcher.Invoke(() =>
          StopButton_PreviewMouseDown(PauseButtonElement, CreateMouseArgs()));
      };

      KeyboardManager.OnContinuePressed = () =>
      {
        Application.Current.Dispatcher.Invoke(() =>
          NextButton_PreviewMouseDown(ContinueButtonElement, CreateMouseArgs()));
      };

      KeyboardManager.OnRepeatPressed = () =>
      {
        Application.Current.Dispatcher.Invoke(() =>
          ReturnMeasureResistanceButton_PreviewMouseDown(RepeatButtonElement, CreateMouseArgs()));
      };
    }

    private MouseButtonEventArgs CreateMouseArgs()
    {
      return new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
      {
        RoutedEvent = UIElement.MouseLeftButtonDownEvent
      };
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

      if (ContinueButtonElement.Visibility == Visibility.Visible)
      {
        EnterStepModeFromPause(isStepInto: false, e);
        return;
      }

      TopLayerButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки BottomLayer.
    /// </summary>
    private void BottomLayer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Вглубь\"");

      if (ContinueButtonElement.Visibility == Visibility.Visible)
      {
        EnterStepModeFromPause(isStepInto: true, e);
        return;
      }

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
      StartButtonElement.PreviewMouseDown += StartMeasureResistanceButton_PreviewMouseDown;
      StopButtonElement.PreviewMouseDown += ExitButton_PreviewMouseDown;

      PauseButtonElement.PreviewMouseDown += StopButton_PreviewMouseDown;
      ContinueButtonElement.PreviewMouseDown += NextButton_PreviewMouseDown;

      StepOverButtonElement.PreviewMouseDown += TopLayer_PreviewMouseDown;
      StepIntoButtonElement.PreviewMouseDown += BottomLayer_PreviewMouseDown;

      RepeatButtonElement.PreviewMouseDown += ReturnMeasureResistanceButton_PreviewMouseDown;
      loopButton.PreviewMouseDown += LoopMeasureResistanceButton_PreviewMouseDown;
    }

    /// <summary>
    /// Скрывает все кнопки управления.
    /// </summary>
    public void SetNonVisibleAllButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        StartButtonElement.Visibility = Visibility.Collapsed;
        PauseButtonElement.Visibility = Visibility.Collapsed;
        ContinueButtonElement.Visibility = Visibility.Collapsed;
        StopButtonElement.Visibility = Visibility.Collapsed;

        RepeatButtonElement.Visibility = Visibility.Collapsed;
        loopButton.Visibility = Visibility.Collapsed;

        StepOverButtonElement.Visibility = Visibility.Collapsed;
        StepIntoButtonElement.Visibility = Visibility.Collapsed;
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
        StartButtonElement.Visibility = Visibility.Visible;
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
        ShowExecutionButtonsOnRunning(ActionExecutor.StepMode);
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
        ShowExecutionButtonsOnRunning(stepMode);
      });
    }

    /// <summary>
    /// Скрывает кнопки режима по шагам.
    /// </summary>
    public void SetNotVisibleStepButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        StepIntoButtonElement.Visibility = Visibility.Collapsed;
        StepOverButtonElement.Visibility = Visibility.Collapsed;
      });
    }

    /// <summary>
    /// Отображает кнопки при паузе.
    /// </summary>
    public void ShowButtonsOnPause(bool repeatVisible = false)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        ShowExecutionButtonsOnPause(ActionExecutor.StepMode, repeatVisible);
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

    public void ShowOnlyExitButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();
        ExitButtonVisibility = Visibility.Visible;

        if (ActionExecutor.StepMode)
        {
          StepOverButtonVisibility = Visibility.Visible;
          StepIntoButtonVisibility = Visibility.Visible;
        }
      });

    }

    public void ShowButtonsOnPause()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        ShowExecutionButtonsOnPause(ActionExecutor.StepMode, repeatVisible: false);
      });
    }

    public void UpdateStepButtonsForCurrentState(bool stepModeEnabled)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        // Для пошагового режима из точки останова всегда нужен сценарий
        // "Продолжить / Завершить", а не "Пауза / Завершить".
        if (StepControlManager.IsBreakpointStepModeActive)
        {
          ShowExecutionButtonsOnPause(stepModeEnabled, repeatVisible: false);
          return;
        }

        if (ContinueButtonElement.Visibility == Visibility.Visible)
        {
          ShowExecutionButtonsOnPause(stepModeEnabled, RepeatButtonElement.Visibility == Visibility.Visible);
          return;
        }

        if (PauseButtonElement.Visibility == Visibility.Visible)
        {
          ShowExecutionButtonsOnRunning(stepModeEnabled);
          return;
        }

        StepIntoButtonElement.Visibility = Visibility.Collapsed;
        StepOverButtonElement.Visibility = Visibility.Collapsed;
      });
    }

    private void ShowExecutionButtonsOnRunning(bool stepMode)
    {
      SetNonVisibleAllButton();
      PauseButtonElement.Visibility = Visibility.Visible;
      StopButtonElement.Visibility = Visibility.Visible;
      StepOverButtonElement.Visibility = stepMode ? Visibility.Visible : Visibility.Collapsed;
      StepIntoButtonElement.Visibility = stepMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowExecutionButtonsOnPause(bool stepMode, bool repeatVisible)
    {
      SetNonVisibleAllButton();
      ContinueButtonElement.Visibility = Visibility.Visible;
      StopButtonElement.Visibility = Visibility.Visible;
      if (repeatVisible)
      {
        RepeatButtonElement.Visibility = Visibility.Visible;
      }

      // На паузе кнопки шага доступны всегда, чтобы можно было перейти в step-mode.
      StepOverButtonElement.Visibility = Visibility.Visible;
      StepIntoButtonElement.Visibility = Visibility.Visible;
    }

    private void EnterStepModeFromPause(bool isStepInto, MouseButtonEventArgs e)
    {
      if (StepControlManager.IsBreakpointStepModeActive)
      {
        ShowButtonsOnPause();
        ExecutionEventAdapter.ExecutionControlEventAdapter.Raise(
          isStepInto ? ExecutionControlButton.StepInto : ExecutionControlButton.StepOver);
        return;
      }

      ExecutionConfig.SetStepByStepMode(true);
      StepControlManager.EnableStepMode(isStepInto);
      ShowButtonsOnPause();
      NextButtonPreviewMouseDown?.Invoke(this, e);
      KeyboardManager.TriggerStep();
    }

    public void StartTask()
    {
      var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
      {
        RoutedEvent = UIElement.PreviewMouseDownEvent,
        Source = StartButtonElement
      };

      StartButtonElement.RaiseEvent(args);
    }

    public void StopTask()
    {
      var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
      {
        RoutedEvent = UIElement.PreviewMouseDownEvent,
        Source = StopButtonElement
      };

      StopButtonElement.RaiseEvent(args);
    }

    public void PauseTask()
    {
      var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
      {
        RoutedEvent = UIElement.PreviewMouseDownEvent,
        Source = PauseButtonElement
      };

      PauseButtonElement.RaiseEvent(args);
    }

    public void NextTask()
    {
      var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
      {
        RoutedEvent = UIElement.PreviewMouseDownEvent,
        Source = ContinueButtonElement
      };

      ContinueButtonElement.RaiseEvent(args);
    }
    #endregion
  }
}
