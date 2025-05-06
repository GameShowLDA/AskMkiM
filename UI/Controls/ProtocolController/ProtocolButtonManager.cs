using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UI.Components.SearchControls;
using UI.Controls.Protocol;

namespace UI.Controls.ProtocolController
{
  public class ProtocolButtonManager
  {
    private readonly Button _startButton;
    private readonly Button _pauseButton;
    private readonly Button _returnButton;
    private readonly Button _loopButton;
    private readonly Button _stepOverButton;
    private readonly Button _stepIntoButton;
    private readonly Button _continueButton;
    private readonly Button _exitButton;

    public ProtocolButtonManager(Protocol protocol)
    {
      _startButton = protocol.startButton;
      _pauseButton = protocol.pauseButton;
      _returnButton = protocol.returnButton;
      _loopButton = protocol.loopButton;
      _stepOverButton = protocol.stepOverButton;
      _stepIntoButton = protocol.stepIntoButton;
      _continueButton = protocol.continueButton;
      _exitButton = protocol.exitButton;
    }

    /// <summary>
    /// Скрывает все кнопки.
    /// </summary>
    public void HideAllButtons()
    {
      SetAllVisibility(Visibility.Collapsed);
    }

    /// <summary>
    /// Отображает только кнопку "Старт", скрывая все остальные кнопки.
    /// </summary>
    public void ShowOnlyStartButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetAllVisibility(Visibility.Collapsed);
        _startButton.Visibility = Visibility.Visible;
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
        SetAllVisibility(Visibility.Collapsed);

        // TODO : Заменить на реальный пошаговый режим
        if (/*ActionExecutor.StepMode */true)
        {
          _stepOverButton.Visibility = Visibility.Visible;
          _stepIntoButton.Visibility = Visibility.Visible;
        }

        _pauseButton.Visibility = Visibility.Visible;
        _exitButton.Visibility = Visibility.Visible;
      });
    }


    /// <summary>
    /// Скрывает кнопки режима по шагам.
    /// </summary>
    public void SetNotVisibleStepButton()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        _stepIntoButton.Visibility = Visibility.Collapsed;
        _stepOverButton.Visibility = Visibility.Collapsed;
      });
    }

    /// <summary>
    /// Отображает кнопки при паузе.
    /// </summary>
    public void ShowButtonsOnPause()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetAllVisibility(Visibility.Collapsed);
        _continueButton.Visibility = Visibility.Visible;
        _exitButton.Visibility = Visibility.Visible;
      });
    }

    /// <summary>
    /// Отображает кнопки при зациклить и повторить.
    /// </summary>
    public void ShowAdditionalFunctionButtons()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetAllVisibility(Visibility.Collapsed);
        _loopButton.Visibility = Visibility.Visible;
        _returnButton.Visibility = Visibility.Visible;
        _exitButton.Visibility = Visibility.Visible;
      });
    }

    private void SetAllVisibility(Visibility visibility)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        _startButton.Visibility = visibility;
        _pauseButton.Visibility = visibility;
        _returnButton.Visibility = visibility;
        _loopButton.Visibility = visibility;
        _stepOverButton.Visibility = visibility;
        _stepIntoButton.Visibility = visibility;
        _continueButton.Visibility = visibility;
        _exitButton.Visibility = visibility;
      });
    }
  }
}
