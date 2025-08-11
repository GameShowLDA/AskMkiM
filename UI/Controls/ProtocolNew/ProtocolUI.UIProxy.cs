using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UI.Controls.ProtocolNew
{
  partial class ProtocolUI
  {
    private UIElement StartButtonElement => IsTopMenuVisible ? (UIElement)StartButtonTop : StartButton;
    private UIElement RepeatButtonElement => IsTopMenuVisible ? (UIElement)RepeatButtonTop : RepeatButton;
    private UIElement StopButtonElement => IsTopMenuVisible ? (UIElement)StopButtonTop : StopButton;
    private UIElement PauseButtonElement => IsTopMenuVisible ? (UIElement)PauseButtonTop : PauseButton;
    private UIElement StepIntoButtonElement => IsTopMenuVisible ? (UIElement)StepIntoTop : StepInto;
    private UIElement StepOverButtonElement => IsTopMenuVisible ? (UIElement)StepOverTop : StepOver;
    private UIElement ContinueButtonElement => IsTopMenuVisible ? (UIElement)ContinueButtonTop : ContinueButton;
  }
}
