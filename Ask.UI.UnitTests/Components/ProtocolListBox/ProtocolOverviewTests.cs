using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.UI.Components.ProtocolListBox;
using Ask.UI.Controls.TextEditorControl;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.UnitTests.Components.ProtocolListBox;

public sealed class ProtocolOverviewTests
{
  [Theory]
  [InlineData(ShowMessageModel.MessageType.Command, true, false, ErrorOverviewSeverity.Information)]
  [InlineData(ShowMessageModel.MessageType.Command, true, true, ErrorOverviewSeverity.Error)]
  [InlineData(ShowMessageModel.MessageType.Command, false, false, ErrorOverviewSeverity.Information)]
  [InlineData(ShowMessageModel.MessageType.CommandBlock, false, false, null)]
  [InlineData(ShowMessageModel.MessageType.Info, false, false, null)]
  public void CommandMarkersIncludeCommandsWithoutStepFlagAndPreserveErrorPriority(
    ShowMessageModel.MessageType status, bool isCommandHeader, bool executionError,
    ErrorOverviewSeverity? expected)
  {
    var message = new ShowMessageModel { Status = status,
      IsControlProgramCommandHeader = isCommandHeader, ExecutionError = executionError };
    Assert.Equal(expected, ProtocolListBoxUI.GetOverviewSeverity(message));
  }

  [Fact]
  public void HoverPreviewContainsSelectedAndAdjacentProtocolLines()
  {
    RunInSta(() =>
    {
      var control = new ProtocolListBoxUI();
      control.LoadMessages(Enumerable.Range(1, 5).Select(i =>
        new ShowMessageModel { Header = $"Строка {i}", Status = ShowMessageModel.MessageType.Command }));

      string preview = control.GetOverviewPreview(0.5)!;

      Assert.Contains("строка 3", preview);
      Assert.Contains("▸", preview);
      Assert.Contains("Строка 2", preview);
      Assert.Contains("Строка 4", preview);
    });
  }

  [Theory]
  [InlineData(ShowMessageModel.MessageType.Info, false, "Количество брака: 0", false)]
  [InlineData(ShowMessageModel.MessageType.Info, false, "[БРАК] R = 20", true)]
  [InlineData(ShowMessageModel.MessageType.Error, false, "Нет связи", true)]
  [InlineData(ShowMessageModel.MessageType.Info, true, "Превышен предел", true)]
  [InlineData(ShowMessageModel.MessageType.Success, false, "[БРАК] отсутствует", false)]
  public void ClassifiesStructuredErrorsAndLegacyMarkers(
    ShowMessageModel.MessageType status, bool executionError, string text, bool expected)
  {
    var message = new ShowMessageModel { MessageColor = Colors.Red, Status = status,
      Header = text, ExecutionError = executionError };
    Assert.Equal(expected, ProtocolListBoxUI.IsOverviewError(message));
  }

  [Fact]
  public void NavigationRevealsErrorInsideCollapsedCommand()
  {
    RunInSta(() =>
    {
      bool original = ProtocolConfig.GetCommandHeadersInProtocol();
      try
      {
        ProtocolConfig.SetCommandHeadersInProtocol(true);
        var command = new ShowMessageModel { Header = "1 ПР", Status = ShowMessageModel.MessageType.Command };
        var error = new ShowMessageModel { Header = "Ошибка", ExecutionError = true };
        var control = new ProtocolListBoxUI();
        control.LoadMessages(new[] { command, error });
        var header = control.DisplayItems[0];
        var group = header.Group!;
        foreach (var body in group.BodyItems) control.DisplayItems.Remove(body);
        group.VisibleBodyCount = 0;
        group.SetExpanded(false);
        ((ListBox)control.FindName("ProtocolListBox")).ItemsSource = control.DisplayItems;

        control.NavigateToErrorOverviewLine(2);

        Assert.True(group.IsExpanded);
        Assert.Contains(control.DisplayItems, item => ReferenceEquals(item.Message, error));
        Assert.Same(error, ((ProtocolDisplayItem)((ListBox)control.FindName("ProtocolListBox")).SelectedItem).Message);
      }
      finally { ProtocolConfig.SetCommandHeadersInProtocol(original); }
    });
  }

  [Fact]
  public void NavigationToAttachedLogSelectsItsOwnerAndExpandsLogs()
  {
    RunInSta(() =>
    {
      var message = new ShowMessageModel { Header = "Измерение", Debug = "[ОТЛАДКА ROOT] источник" };
      var log = new ShowMessageModel { Debug = "[ЛОГ ROOT] ответ прибора" };
      var control = new ProtocolListBoxUI();
      control.LoadMessages(new[] { message, log });
      control.NavigateToErrorOverviewLine(2);
      var item = Assert.Single(control.DisplayItems);
      Assert.Same(message, item.Message);
      Assert.True(item.AreServiceLogsExpanded);
    });
  }

  [Fact]
  public void ProtocolHostExposesOverviewConfigurationProperties()
  {
    RunInSta(() =>
    {
      var barStyle = new System.Windows.Style(typeof(ErrorOverviewBar));
      var hostStyle = new System.Windows.Style(typeof(Border));
      var control = new ProtocolListBoxUI
      {
        OverviewBarStyle = barStyle,
        OverviewHostStyle = hostStyle,
        OverviewVisibility = System.Windows.Visibility.Collapsed,
        ProtocolVerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
      };

      Assert.Same(barStyle, control.OverviewBarStyle);
      Assert.Same(hostStyle, control.OverviewHostStyle);
      Assert.Equal(System.Windows.Visibility.Collapsed, control.OverviewVisibility);
      Assert.Equal(ScrollBarVisibility.Hidden, control.ProtocolVerticalScrollBarVisibility);
    });
  }

  [Fact]
  public void DenseMarkersStayNearTheirPositionAndCycleWithinCluster()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar();
      bar.Measure(new System.Windows.Size(24, 100));
      bar.Arrange(new System.Windows.Rect(0, 0, 24, 100));
      int selected = 0;
      bar.SetLineDiagnostics(100, Enumerable.Range(1, 100)
        .Select(i => (i, ErrorOverviewSeverity.Error, $"Ошибка {i}")), line => selected = line);
      bar.NavigateAt(50, false);
      int first = selected;
      Assert.InRange(first, 45, 55);
      bar.NavigateAt(50, false);
      Assert.Equal(first + 1, selected);
      bar.NavigateAt(50, true);
      Assert.Equal(first, selected);
    });
  }

  [Fact]
  public void EmptyTrackNavigatesToDocumentPosition()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar();
      bar.Measure(new System.Windows.Size(24, 100));
      bar.Arrange(new System.Windows.Rect(0, 0, 24, 100));
      int selected = 0;
      bar.SetLineDiagnostics(100, null, line => selected = line);
      bar.NavigateAt(0, false);
      Assert.Equal(1, selected);
      bar.NavigateAt(100, false);
      Assert.Equal(100, selected);
    });
  }

  [Theory]
  [InlineData(0, 0)]
  [InlineData(1, 20)]
  [InlineData(2, 220)]
  [InlineData(3, 240)]
  [InlineData(4, 440)]
  [InlineData(5, 640)]
  public void ProjectionUsesMeasuredVariableHeightsAndInterpolatesUnrealizedItems(int index, double expected)
  {
    var anchors = new[] { (0, 0d), (1, 20d), (2, 220d), (3, 240d), (5, 640d) };
    Assert.Equal(expected, ProtocolListBoxUI.ProjectOverviewOffset(index, anchors));
  }

  [Fact]
  public void ProjectedMarkersNavigateByIdentityInsteadOfLogicalFraction()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar();
      bar.Measure(new System.Windows.Size(24, 100));
      bar.Arrange(new System.Windows.Rect(0, 0, 24, 100));
      int selected = 0;
      double scrolled = -1;
      bar.SetLineDiagnostics(100, new[] { (2, ErrorOverviewSeverity.Information, "ПИ/ПИ1") },
        line => selected = line);
      bar.SetLinePositions(new Dictionary<int, double> { [2] = 0.8 }, fraction => scrolled = fraction);
      bar.NavigateAt(78, false);
      Assert.Equal(2, selected);
      bar.NavigateAt(50, false);
      Assert.Equal(0.5, scrolled);
    });
  }

  [Fact]
  public void NavigationCanDisableTrackIndependentlyOfMarkers()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar { IsTrackNavigationEnabled = false };
      bar.Measure(new System.Windows.Size(24, 100));
      bar.Arrange(new System.Windows.Rect(0, 0, 24, 100));
      int selected = 0;
      bar.SetLineDiagnostics(100, new[] { (1, ErrorOverviewSeverity.Error, "Ошибка") },
        line => selected = line);
      bar.NavigateAt(50, false);
      Assert.Equal(0, selected);
      bar.NavigateAt(0, false);
      Assert.Equal(1, selected);
      selected = 0;
      bar.IsNavigationEnabled = false;
      bar.NavigateAt(0, false);
      Assert.Equal(0, selected);
    });
  }

  [Fact]
  public void HiddenSeverityDoesNotConsumeVisibleCommandOnSameLine()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar { IsTrackNavigationEnabled = false };
      bar.Measure(new System.Windows.Size(24, 100));
      bar.Arrange(new System.Windows.Rect(0, 0, 24, 100));
      int selected = 0;
      bar.SetLineDiagnostics(100, new[] {
        (1, ErrorOverviewSeverity.Error, "Ошибка"),
        (1, ErrorOverviewSeverity.Information, "Команда") }, line => selected = line);
      bar.AreErrorMarkersVisible = false;
      bar.NavigateAt(0, false);
      Assert.Equal(1, selected);
      selected = 0;
      bar.AreCommandMarkersVisible = false;
      bar.NavigateAt(0, false);
      Assert.Equal(0, selected);
      bar.AreErrorMarkersVisible = true;
      bar.NavigateAt(0, false);
      Assert.Equal(1, selected);
    });
  }

  [Fact]
  public void XamlStylesAndDynamicResourcesReachRenderingAndPreview()
  {
    RunInSta(() =>
    {
      var bar = (ErrorOverviewBar)System.Windows.Markup.XamlReader.Parse("""
        <overview:ErrorOverviewBar xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:overview="clr-namespace:Ask.UI.Controls.TextEditorControl;assembly=Ask.UI">
          <overview:ErrorOverviewBar.Resources>
            <SolidColorBrush x:Key="CommandColor" Color="Lime"/>
          </overview:ErrorOverviewBar.Resources>
          <overview:ErrorOverviewBar.Style>
            <Style TargetType="overview:ErrorOverviewBar">
              <Setter Property="CommandBrush" Value="{DynamicResource CommandColor}"/>
              <Setter Property="PreviewWidth" Value="320"/>
              <Setter Property="PreviewTextWrapping" Value="Wrap"/>
              <Setter Property="MarkerHeight" Value="8"/>
              <Setter Property="IsViewportVisible" Value="False"/>
            </Style>
          </overview:ErrorOverviewBar.Style>
        </overview:ErrorOverviewBar>
        """);
      bar.Measure(new System.Windows.Size(24, 100));
      bar.Arrange(new System.Windows.Rect(0, 0, 24, 100));
      bar.SetLineDiagnostics(100, new[] { (1, ErrorOverviewSeverity.Information, "Команда") });
      Assert.Equal(Colors.Lime, ((SolidColorBrush)bar.CommandBrush).Color);
      bar.Resources["CommandColor"] = Brushes.Yellow;
      Assert.Equal(Colors.Yellow, ((SolidColorBrush)bar.CommandBrush).Color);
      var preview = (Border)bar.FindName("_previewBorder");
      Assert.Equal(320, preview.Width);
      var content = (ContentControl)bar.FindName("_previewContent");
      content.Content = "Длинная строка предпросмотра";
      preview.Measure(new System.Windows.Size(320, 100));
      preview.Arrange(new System.Windows.Rect(0, 0, 320, 100));
      System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { },
        System.Windows.Threading.DispatcherPriority.ApplicationIdle);
      var text = FindDescendant<TextBlock>(content);
      Assert.NotNull(text);
      Assert.Equal(System.Windows.TextWrapping.Wrap, text.TextWrapping);
      var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(24, 100, 96, 96, PixelFormats.Pbgra32);
      bitmap.Render(bar);
      var pixel = new byte[4];
      bitmap.CopyPixels(new System.Windows.Int32Rect(10, 2, 1, 1), pixel, 4, 0);
      Assert.Equal(new byte[] { 0, 255, 255, 255 }, pixel);
    });
  }

  [Fact]
  public void InvalidGeometryIsRejectedBeforeRendering()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar();
      Assert.Throws<ArgumentException>(() => bar.MarkerHeight = 0);
      Assert.Throws<ArgumentException>(() => bar.MarkerGap = -1);
      Assert.Throws<ArgumentException>(() => bar.PreviewWidth = double.NaN);
      Assert.Throws<ArgumentException>(() => bar.ViewportOpacity = 1.1);
    });
  }

  [Fact]
  public void DisabledTooltipsDoNotRequestPreviewContent()
  {
    RunInSta(() =>
    {
      var bar = new ErrorOverviewBar { AreToolTipsEnabled = false };
      bool requested = false;
      bar.SetPositionPreviewFactory(_ => { requested = true; return null; });
      bar.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0)
        { RoutedEvent = System.Windows.Input.Mouse.MouseMoveEvent });
      Assert.False(requested);
      Assert.False(((System.Windows.Controls.Primitives.Popup)bar.FindName("_previewPopup")).IsOpen);
      bar.AreToolTipsEnabled = true;
      bar.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0)
        { RoutedEvent = System.Windows.Input.Mouse.MouseMoveEvent });
      Assert.True(requested);
    });
  }

  [Fact]
  public void RightScrollBarTracksViewerAndScrollsProtocol()
  {
    RunInSta(() =>
    {
      var control = new ProtocolListBoxUI();
      var scrollStyle = new System.Windows.Style(typeof(System.Windows.Controls.Primitives.ScrollBar));
      scrollStyle.Setters.Add(new System.Windows.Setter(System.Windows.FrameworkElement.MarginProperty,
        new System.Windows.Thickness(0, 0, 5, 0)));
      control.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = scrollStyle;
      control.LoadMessages(Enumerable.Range(1, 200).Select(i =>
        new ShowMessageModel { Header = $"Строка {i}", Message = "Результат измерения", Status = ShowMessageModel.MessageType.Info }));
      void Layout()
      {
        control.Measure(new System.Windows.Size(800, 400));
        control.Arrange(new System.Windows.Rect(0, 0, 800, 400));
        control.UpdateLayout();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { },
          System.Windows.Threading.DispatcherPriority.ApplicationIdle);
      }
      Layout();
      control.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.FrameworkElement.LoadedEvent));
      try
      {
        Layout();
        var viewer = FindDescendant<ScrollViewer>((ListBox)control.FindName("ProtocolListBox"))!;
        var scroll = (System.Windows.Controls.Primitives.ScrollBar)control.FindName("ProtocolVerticalScrollBar");
        var overview = (Border)control.FindName("OverviewTrackHost");
        Assert.True(scroll.Maximum > 0);
        Assert.Equal(System.Windows.Visibility.Visible, scroll.Visibility);
        var overviewBounds = new System.Windows.Rect(overview.TranslatePoint(new System.Windows.Point(), control), overview.RenderSize);
        var scrollBounds = new System.Windows.Rect(scroll.TranslatePoint(new System.Windows.Point(), control), scroll.RenderSize);
        Assert.True(overviewBounds.IntersectsWith(scrollBounds));
        Assert.Equal(overviewBounds.X + overviewBounds.Width / 2,
          scrollBounds.X + scrollBounds.Width / 2, precision: 3);
        Assert.True(Panel.GetZIndex(overview) > Panel.GetZIndex(scroll));
        Assert.True(overview.IsHitTestVisible);
        Assert.Null(overview.Background);
        Assert.True(((ErrorOverviewBar)control.FindName("errorOverviewBar")).IsMarkerHitTestOnly);
        viewer.ScrollToVerticalOffset(100);
        Layout();
        Assert.Equal(viewer.VerticalOffset, scroll.Value);
        Assert.Equal(viewer.ViewportHeight, scroll.ViewportSize);
        scroll.RaiseEvent(new System.Windows.Controls.Primitives.ScrollEventArgs(
          System.Windows.Controls.Primitives.ScrollEventType.ThumbTrack, 200));
        Layout();
        Assert.Equal(200, viewer.VerticalOffset);
        Assert.Equal(viewer.VerticalOffset, scroll.Value);
      }
      finally
      {
        control.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.FrameworkElement.UnloadedEvent));
      }
    });
  }

  [Fact]
  public void MarkerOverlayReceivesMarkerHitsAndPassesEmptyTrackToScrollBar()
  {
    RunInSta(() =>
    {
      var scroll = new System.Windows.Controls.Primitives.ScrollBar { Maximum = 100, ViewportSize = 20 };
      var bar = new ErrorOverviewBar { Background = Brushes.Transparent,
        IsViewportVisible = false, IsMarkerHitTestOnly = true, MarkerHeight = 8 };
      var host = new Border { Child = bar };
      var grid = new Grid();
      grid.Children.Add(scroll);
      grid.Children.Add(host);
      Panel.SetZIndex(host, 2);
      using var source = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters("Marker hit testing")
      {
        Width = 30, Height = 200, WindowStyle = unchecked((int)0x80000000),
      });
      source.RootVisual = grid;
      grid.Measure(new System.Windows.Size(30, 200));
      grid.Arrange(new System.Windows.Rect(0, 0, 30, 200));
      int selected = 0;
      bar.SetLineDiagnostics(100, new[] { (1, ErrorOverviewSeverity.Error, "Ошибка") },
        line => selected = line);
      var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(30, 200, 96, 96, PixelFormats.Pbgra32);
      bitmap.Render(grid);
      Assert.Same(bar.FindName("OverviewSurface"), grid.InputHitTest(new System.Windows.Point(15, 3)));
      bar.NavigateAt(3, false);
      Assert.Equal(1, selected);
      var emptyHit = grid.InputHitTest(new System.Windows.Point(15, 100)) as System.Windows.DependencyObject;
      Assert.NotNull(emptyHit);
      Assert.True(ReferenceEquals(scroll, emptyHit) || scroll.IsAncestorOf(emptyHit));
      bar.AreErrorMarkersVisible = false;
      bitmap.Render(grid);
      var hiddenHit = grid.InputHitTest(new System.Windows.Point(15, 3)) as System.Windows.DependencyObject;
      Assert.NotNull(hiddenHit);
      Assert.True(ReferenceEquals(scroll, hiddenHit) || scroll.IsAncestorOf(hiddenHit));
    });
  }

  private static T? FindDescendant<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
  {
    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
    {
      var child = VisualTreeHelper.GetChild(parent, i);
      if (child is T match) return match;
      if (FindDescendant<T>(child) is { } descendant) return descendant;
    }
    return null;
  }

  private static void RunInSta(Action action)
  {
    Exception? failure = null;
    var thread = new Thread(() =>
    {
      try { action(); }
      catch (Exception ex) { failure = ex; }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (failure != null) throw new InvalidOperationException(failure.ToString(), failure);
  }
}
