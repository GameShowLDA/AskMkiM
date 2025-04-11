using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI.Components.SearchControls;

namespace UI.Controls.ProtocolController
{
  /// <summary>
  /// Отвечает за управление отображением кнопок в интерфейсе ProtocolController.
  /// Позволяет скрывать и показывать элементы в зависимости от текущего состояния.
  /// </summary>
  public partial class ProtocolController
  {
    #region Свойства.

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

    #region Методы.

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
      });
    }

    /// <summary>
    /// Отображает только кнопку "Запустить", скрывая все остальные.
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
    /// Отображает кнопки "Остановить" и "Завершить", а также кнопки пошагового режима при необходимости.
    /// </summary>
    public void ShowOnlyStopAndFinishButtons()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();

        if (ActionExecutor.StepMode)
        {
          stepOverButton.Visibility = Visibility.Visible;
          stepIntoButton.Visibility = Visibility.Visible;
        }

        pauseButton.Visibility = Visibility.Visible;
        exitButton.Visibility = Visibility.Visible;
      });
    }

    /// <summary>
    /// Отображает кнопки "Остановить" и "Завершить", а также пошаговые кнопки, если указан режим.
    /// </summary>
    /// <param name="stepMode">Флаг пошагового режима.</param>
    public void ShowOnlyStopAndFinishButtons(bool stepMode)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();

        if (stepMode)
        {
          stepOverButton.Visibility = Visibility.Visible;
          stepIntoButton.Visibility = Visibility.Visible;
        }

        pauseButton.Visibility = Visibility.Visible;
        exitButton.Visibility = Visibility.Visible;
      });
    }

    /// <summary>
    /// Скрывает кнопки пошагового режима (F10 и F11).
    /// </summary>
    public void SetNotVisibleStepButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        stepOverButton.Visibility = Visibility.Collapsed;
        stepIntoButton.Visibility = Visibility.Collapsed;
      });
    }

    /// <summary>
    /// Отображает кнопки, используемые во время паузы.
    /// </summary>
    public void ShowButtonsOnPause()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();
        continueButton.Visibility = Visibility.Visible;
        exitButton.Visibility = Visibility.Visible;
      });
    }

    /// <summary>
    /// Отображает дополнительные кнопки "Повторить", "Зациклить" и "Завершить".
    /// </summary>
    public void ShowAdditionalFunctionButtons()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetNonVisibleAllButton();
        loopButton.Visibility = Visibility.Visible;
        returnButton.Visibility = Visibility.Visible;
        exitButton.Visibility = Visibility.Visible;
      });
    }

    #endregion
  }
}
