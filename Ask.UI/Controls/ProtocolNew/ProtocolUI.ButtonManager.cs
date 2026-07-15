using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;
using Ask.UI.Features.ProtocolNew.Controls;
using System.Windows;
using System.Windows.Input;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Controls.ProtocolNew
{
  public partial class ProtocolUI : IButtonService, IProtocolButtonView
  {
    /// <summary>
    /// Контроллер, централизованно применяющий состояния панели кнопок.
    /// </summary>
    private ProtocolButtonController _buttonController = null!;

    private TaskCompletionSource<bool>? _adminButtonTcs;
    private bool _startRequestedInStepMode;

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

    /// <inheritdoc />
    Visibility IProtocolButtonView.StartVisibility
    {
      get => StartButtonElement.Visibility;
      set => StartButtonElement.Visibility = value;
    }

    /// <inheritdoc />
    Visibility IProtocolButtonView.PauseVisibility
    {
      get => PauseButtonElement.Visibility;
      set => PauseButtonElement.Visibility = value;
    }

    /// <inheritdoc />
    Visibility IProtocolButtonView.ContinueVisibility
    {
      get => ContinueButtonElement.Visibility;
      set => ContinueButtonElement.Visibility = value;
    }

    /// <inheritdoc />
    Visibility IProtocolButtonView.ExitVisibility
    {
      get => StopButtonElement.Visibility;
      set => StopButtonElement.Visibility = value;
    }

    /// <inheritdoc />
    Visibility IProtocolButtonView.RepeatVisibility
    {
      get => RepeatButtonElement.Visibility;
      set => RepeatButtonElement.Visibility = value;
    }

    /// <inheritdoc />
    Visibility IProtocolButtonView.LoopVisibility
    {
      get => loopButton.Visibility;
      set => loopButton.Visibility = value;
    }

    /// <inheritdoc />
    Visibility IProtocolButtonView.StepOverVisibility
    {
      get => StepOverButtonElement.Visibility;
      set => StepOverButtonElement.Visibility = value;
    }

    /// <inheritdoc />
    Visibility IProtocolButtonView.StepIntoVisibility
    {
      get => StepIntoButtonElement.Visibility;
      set => StepIntoButtonElement.Visibility = value;
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

      // Режим шага выбирается только в момент запуска:
      // F10/F11 выставляют флаг заранее, обычный старт сбрасывает его.
      var startInStepMode = _startRequestedInStepMode;
      _startRequestedInStepMode = false;
      ExecutionConfig.SetStepByStepMode(startInStepMode);

      SetNonVisibleAllButton();
      ShowOnlyStopAndFinishButtons(startInStepMode);
      StartMeasureResistanceButtonPreviewMouseDown?.Invoke(this, e);
    }

    /// <summary>
    /// Обработчик события PreviewMouseDown для кнопки StopButton.
    /// </summary>
    private void StopButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Остановить\"");

      // Фиксируем запрос паузы сразу в executor, чтобы избежать гонки
      // между быстрым "Пауза -> Продолжить" и запуском async-обработчика.
      ActionExecutor.RequestPause();
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

      ShowOnlyStartButton();
      ExitButtonPreviewMouseDown?.Invoke(this, e);
    }
    private void RegisterHotkeys()
    {
      KeyboardManager.OnRunOrPausePressed = HandleRunOrPause;

      KeyboardManager.OnStartPressed = () =>
        Application.Current.Dispatcher.Invoke(() =>
        {
          _startRequestedInStepMode = false;
          ExecutionConfig.SetStepByStepMode(false);
          StartMeasureResistanceButton_PreviewMouseDown(StartButtonElement, CreateMouseArgs());
        });

      KeyboardManager.OnStartPressedByStepMode = () =>
        Application.Current.Dispatcher.Invoke(() =>
        {
          _startRequestedInStepMode = true;
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
      _buttonController = new ProtocolButtonController(this);
      SetupEventHandlers();
      ShowOnlyStartButton();
      HideProtocolManager();
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
      _buttonController.Apply(ProtocolButtonState.Hidden);
    }

    /// <summary>
    /// Отображает только кнопку "Старт", скрывая все остальные кнопки.
    /// </summary>
    public void ShowOnlyStartButton()
    {
      _buttonController.Apply(ProtocolButtonState.Ready);
    }

    /// <summary>
    /// Отображает кнопки при выполнении.
    /// </summary>
    /// <param name="stepMode">Режим по шагам.</param>
    public void ShowOnlyStopAndFinishButtons()
    {
      _buttonController.Apply(ProtocolButtonState.Running, ActionExecutor.StepMode);
    }

    /// <summary>
    /// Отображает кнопки при выполнении.
    /// </summary>
    /// <param name="stepMode">Режим по шагам.</param>
    public void ShowOnlyStopAndFinishButtons(bool stepMode)
    {
      _buttonController.Apply(ProtocolButtonState.Running, stepMode);
    }

    /// <summary>
    /// Скрывает кнопки режима по шагам.
    /// </summary>
    public void SetNotVisibleStepButton()
    {
      _buttonController.HideStepButtons();
    }

    /// <summary>
    /// Отображает кнопки при паузе.
    /// </summary>
    public void ShowButtonsOnPause(bool repeatVisible = false)
    {
      _buttonController.Apply(ProtocolButtonState.Paused, ActionExecutor.StepMode, repeatVisible);
    }

    /// <summary>
    /// Отображает кнопки при зациклить и повторить.
    /// </summary>
    public void ShowAdditionalFunctionButtons()
    {
      _buttonController.Apply(ProtocolButtonState.AdditionalActions);
    }

    public void ShowOnlyExitButton()
    {
      _buttonController.Apply(ProtocolButtonState.ExitOnly, ActionExecutor.StepMode);
    }

    public void ShowButtonsOnPause()
    {
      _buttonController.Apply(ProtocolButtonState.Paused, ActionExecutor.StepMode);
    }

    public void UpdateStepButtonsForCurrentState(bool stepModeEnabled)
    {
      _buttonController.UpdateStepMode(
        stepModeEnabled,
        StepControlManager.IsBreakpointStepModeActive);
    }

    /// <summary>
    /// Скрывает элементы управления активным выполнением при сбросе состояния исполнителя.
    /// </summary>
    internal void HideExecutionButtonsAfterReset()
    {
      _buttonController.HideExecutionControls();
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
      NextButtonPreviewMouseDown?.Invoke(this, e);
      KeyboardManager.TriggerStep();
      ShowOnlyStopAndFinishButtons(isStepInto);
    }

    public void ShowProtocolManager()
    {
      Application.Current.Dispatcher.Invoke(() => ProtocolManager.Visibility = Visibility.Visible);
    }

    public void HideProtocolManager()
    {
      Application.Current.Dispatcher.Invoke(() => ProtocolManager.Visibility = Visibility.Collapsed);
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
