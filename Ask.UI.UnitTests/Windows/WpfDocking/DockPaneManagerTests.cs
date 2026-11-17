using UI.Windows.WpfDocking.Windows.Docking;

namespace Ask.UI.UnitTests.Windows.WpfDocking;

public sealed class DockPaneManagerTests
{
  [Theory]
  [InlineData(true, DockPosition.Document, false)]
  [InlineData(true, DockPosition.Floating, true)]
  [InlineData(false, DockPosition.Floating, false)]
  [InlineData(true, DockPosition.Hidden, false)]
  public void RequiresNativeFloatingWindowPump_OnlyForNativeFloatingItems(
    bool canBeNative,
    DockPosition dockPosition,
    bool expected)
  {
    Assert.Equal(expected, DockPaneManager.RequiresNativeFloatingWindowPump(canBeNative, dockPosition));
  }
}
