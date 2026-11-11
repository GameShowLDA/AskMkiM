using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using UI.Windows.WpfDocking.Windows;

namespace Ask.UI.UnitTests.Controls.Docking;

public sealed class SplitContainerMeasureTests
{
  [Theory]
  [InlineData(Orientation.Horizontal, true, true)]
  [InlineData(Orientation.Horizontal, false, true)]
  [InlineData(Orientation.Vertical, true, true)]
  [InlineData(Orientation.Vertical, false, true)]
  [InlineData(Orientation.Horizontal, true, false)]
  [InlineData(Orientation.Horizontal, false, false)]
  [InlineData(Orientation.Vertical, true, false)]
  [InlineData(Orientation.Vertical, false, false)]
  public void FiniteLayout_MeasuresEachChildOnceInItsAllocatedSlot(
    Orientation orientation, bool sizeFirstChild, bool proportional) => RunInSta(() =>
  {
    var first = new MeasureProbe(new Size(80, 40));
    var second = new MeasureProbe(new Size(120, 60));
    var split = new SplitContainer
    {
      Child1 = first,
      Child2 = second,
      Orientation = orientation,
      IsSplitterTopLeft = sizeFirstChild,
      SplitterDistance = proportional ? new SplitterDistance(1, SplitterUnitType.Star) : new SplitterDistance(120),
    };

    var available = new Size(600, 400);
    split.Measure(available);
    split.Arrange(new Rect(available));

    var length = orientation == Orientation.Horizontal ? available.Width : available.Height;
    var sizedLength = proportional ? (length - split.SplitterWidth) / 2 : 120;
    var firstLength = sizeFirstChild ? sizedLength : length - split.SplitterWidth - sizedLength;
    var secondLength = length - split.SplitterWidth - firstLength;
    Assert.Equal(orientation == Orientation.Horizontal ? new Size(firstLength, 400) : new Size(600, firstLength),
      Assert.Single(first.Constraints));
    Assert.Equal(orientation == Orientation.Horizontal ? new Size(secondLength, 400) : new Size(600, secondLength),
      Assert.Single(second.Constraints));
  });

  [Theory]
  [InlineData(Orientation.Horizontal, true, false)]
  [InlineData(Orientation.Horizontal, false, false)]
  [InlineData(Orientation.Vertical, true, false)]
  [InlineData(Orientation.Vertical, false, false)]
  [InlineData(Orientation.Horizontal, true, true)]
  [InlineData(Orientation.Horizontal, false, true)]
  [InlineData(Orientation.Vertical, true, true)]
  [InlineData(Orientation.Vertical, false, true)]
  public void AutoLayout_OnlyMeasuresTheSizedChildNaturallyAndBoundsTheCrossAxis(
    Orientation orientation, bool sizeFirstChild, bool proportional) => RunInSta(() =>
  {
    var first = new MeasureProbe(new Size(80, 40));
    var second = new MeasureProbe(new Size(120, 60));
    var split = new SplitContainer
    {
      Child1 = first,
      Child2 = second,
      Orientation = orientation,
      IsSplitterTopLeft = sizeFirstChild,
      SplitterDistance = proportional ? SplitterDistance.AutoStar : SplitterDistance.AutoPixel,
    };

    split.Measure(new Size(600, 400));

    var sized = sizeFirstChild ? first : second;
    var other = sizeFirstChild ? second : first;
    Assert.Equal(2, sized.Constraints.Count);
    Assert.Equal(orientation == Orientation.Horizontal
      ? new Size(double.PositiveInfinity, 400) : new Size(600, double.PositiveInfinity), sized.Constraints[0]);
    Assert.Single(other.Constraints);
    Assert.All(other.Constraints, constraint =>
    {
      Assert.True(double.IsFinite(constraint.Width));
      Assert.True(double.IsFinite(constraint.Height));
    });
    Assert.Equal(sizeFirstChild ? new Size(80, 40) : new Size(120, 60), sized.DesiredSize);
  });

  [Theory]
  [InlineData(Orientation.Horizontal)]
  [InlineData(Orientation.Vertical)]
  public void UnboundedCrossAxisAndFullyUnboundedParent_ReturnFiniteNaturalSizes(Orientation orientation) => RunInSta(() =>
  {
    var split = new SplitContainer
    {
      Child1 = new MeasureProbe(new Size(80, 40)),
      Child2 = new MeasureProbe(new Size(120, 60)),
      Orientation = orientation,
    };
    split.Measure(orientation == Orientation.Horizontal
      ? new Size(600, double.PositiveInfinity) : new Size(double.PositiveInfinity, 400));
    Assert.Equal(orientation == Orientation.Horizontal ? new Size(600, 60) : new Size(120, 400), split.DesiredSize);

    split.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
    Assert.Equal(orientation == Orientation.Horizontal ? new Size(204, 60) : new Size(120, 104), split.DesiredSize);
  });

  [Theory]
  [InlineData(Orientation.Horizontal)]
  [InlineData(Orientation.Vertical)]
  public void UnboundedParent_UsesBothNaturalSizes(Orientation orientation) => RunInSta(() =>
  {
    var first = new MeasureProbe(new Size(80, 40));
    var second = new MeasureProbe(new Size(120, 60));
    var split = new SplitContainer { Child1 = first, Child2 = second, Orientation = orientation };
    var available = orientation == Orientation.Horizontal
      ? new Size(double.PositiveInfinity, 400) : new Size(600, double.PositiveInfinity);

    split.Measure(available);

    Assert.Equal(orientation == Orientation.Horizontal ? new Size(204, 400) : new Size(600, 104), split.DesiredSize);
    Assert.Equal(available, first.Constraints[0]);
    Assert.Equal(available, second.Constraints[0]);
  });

  [Fact]
  public void NestedFiniteSplits_DoNotMultiplyDescendantMeasurements() => RunInSta(() =>
  {
    var leaf = new MeasureProbe(new Size(80, 40));
    UIElement content = leaf;
    for (var depth = 0; depth < 8; depth++)
    {
      content = new SplitContainer
      {
        Child1 = content,
        Child2 = new MeasureProbe(new Size(80, 40)),
        Orientation = depth % 2 == 0 ? Orientation.Horizontal : Orientation.Vertical,
      };
    }

    content.Measure(new Size(1600, 1000));

    var constraint = Assert.Single(leaf.Constraints);
    Assert.True(double.IsFinite(constraint.Width));
    Assert.True(double.IsFinite(constraint.Height));
  });

  private static void RunInSta(Action action)
  {
    Exception? failure = null;
    var thread = new Thread(() =>
    {
      try { action(); }
      catch (Exception exception) { failure = exception; }
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "SplitContainer measurement timed out");
    if (failure != null)
      ExceptionDispatchInfo.Capture(failure).Throw();
  }

  private sealed class MeasureProbe(Size naturalSize) : FrameworkElement
  {
    public List<Size> Constraints { get; } = new();

    protected override Size MeasureOverride(Size availableSize)
    {
      Constraints.Add(availableSize);
      return new Size(Math.Min(naturalSize.Width, availableSize.Width), Math.Min(naturalSize.Height, availableSize.Height));
    }
  }
}
