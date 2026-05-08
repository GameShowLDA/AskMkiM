using System.Windows;

namespace Ask.UI.Controls.ProtocolNew
{
  public partial class ProtocolUI
  {
    private UIElement StartButtonElement => IsTopMenuVisible ? StartButtonTop : StartButton;
    private UIElement RepeatButtonElement => IsTopMenuVisible ? RepeatButtonTop : RepeatButton;
    private UIElement StopButtonElement => IsTopMenuVisible ? StopButtonTop : StopButton;
    private UIElement PauseButtonElement => IsTopMenuVisible ? PauseButtonTop : PauseButton;
    private UIElement StepIntoButtonElement => IsTopMenuVisible ? StepIntoTop : StepInto;
    private UIElement StepOverButtonElement => IsTopMenuVisible ? StepOverTop : StepOver;
    private UIElement ContinueButtonElement => IsTopMenuVisible ? ContinueButtonTop : ContinueButton;
  }
}
