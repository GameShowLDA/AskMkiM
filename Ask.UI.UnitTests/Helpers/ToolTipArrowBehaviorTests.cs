using System.Windows;
using System.Windows.Controls;
using UI.Helpers;

namespace Ask.UI.UnitTests.Helpers;

public sealed class ToolTipArrowBehaviorTests
{
  [Fact]
  public void UpdateArrow_ClosedDisconnectedToolTip_DoesNotRequestScreenCoordinates()
    => RunInSta(() =>
    {
      var target = new Border();
      var toolTip = new ToolTip
      {
        Content = "Подсказка",
        PlacementTarget = target,
      };

      target.Measure(new Size(120, 32));
      target.Arrange(new Rect(0, 0, 120, 32));
      toolTip.Measure(new Size(240, 80));
      toolTip.Arrange(new Rect(0, 0, 240, 80));

      Assert.False(toolTip.IsOpen);
      Assert.Null(PresentationSource.FromVisual(toolTip));

      var exception = Record.Exception(() => ToolTipArrowBehavior.UpdateArrow(toolTip));

      Assert.Null(exception);
    });

  private static void RunInSta(Action action)
  {
    Exception? exception = null;
    var thread = new Thread(() =>
    {
      try
      {
        action();
      }
      catch (Exception error)
      {
        exception = error;
      }
    }) { IsBackground = true };

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(15)));

    if (exception != null)
    {
      throw new InvalidOperationException("Проверка поведения подсказки завершилась ошибкой.", exception);
    }
  }
}
