using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using System.Collections;
using System.Reflection;

namespace Ask.UI.UnitTests.Services.TextSearch;

public sealed class ReplacementMarkerTests
{
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void ReplacementRanges_SurviveSearchRefreshAndFollowSubsequentEdits(bool sharedEditor)
  {
    RunInSta(() =>
    {
      var editor = new TextEditor { Text = "one two three" };
      var markers = CreateMarkers(editor, sharedEditor);
      markers.AddReplacement(8, 5);
      markers.AddSearch(0, 3, true);
      markers.Clear(true);
      var range = Assert.Single(GetRanges(markers.Service));
      Assert.Equal((8, 5), (range.StartOffset, range.Length));

      editor.Document.Replace(0, 3, "123456");
      range = Assert.Single(GetRanges(markers.Service));
      Assert.Equal((11, 5), (range.StartOffset, range.Length));
      markers.AddReplacement(0, 6);
      markers.AddSearch(7, 3, false);
      markers.Clear(true);
      Assert.Equal(new[] { (0, 6), (11, 5) }, GetRanges(markers.Service)
        .OrderBy(item => item.StartOffset).Select(item => (item.StartOffset, item.Length)));
      markers.ClearReplacements();
      Assert.Empty(GetRanges(markers.Service));
    });
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void ReplacementCleanup_PreservesFoundMatchesAndHandlesOverlappingReplacements(bool sharedEditor)
  {
    RunInSta(() =>
    {
      var editor = new TextEditor { Text = "cat cat" };
      var markers = CreateMarkers(editor, sharedEditor);
      markers.AddReplacement(0, 3);
      markers.AddReplacement(0, 3);
      Assert.Single(GetRanges(markers.Service));
      markers.AddSearch(4, 3, false);
      markers.ClearReplacements();
      var range = Assert.Single(GetRanges(markers.Service));
      Assert.Equal((4, 3), (range.StartOffset, range.Length));
      markers.Clear(false);
      Assert.Empty(GetRanges(markers.Service));
    });
  }

  [Theory]
  [InlineData(false, false)]
  [InlineData(false, true)]
  [InlineData(true, false)]
  [InlineData(true, true)]
  public void Review_ResolvesOnlyThisReplacementAfterUnrelatedEdits(bool sharedEditor, bool cancel)
  {
    RunInSta(() =>
    {
      var editor = new TextEditor { Text = "dog other" };
      var markers = CreateMarkers(editor, sharedEditor);
      markers.Service.GetType().GetMethod("AddReplacementMarker", new[] { typeof(int), typeof(int), typeof(string) })!
        .Invoke(markers.Service, new object[] { 0, 3, "cat" });
      editor.Document.Insert(0, "++ ");
      editor.Document.Insert(editor.Document.TextLength, " changed");
      var segment = Assert.Single(GetRanges(markers.Service));
      var review = (Ask.UI.Shared.Search.ReplacementReview)segment.GetType().GetProperty("Review")!.GetValue(segment)!;
      Assert.True(review.CanCancel);
      Assert.True(review.Resolve(cancel));
      Assert.Equal(cancel ? "++ cat other changed" : "++ dog other changed", editor.Text);
      Assert.Empty(GetRanges(markers.Service));
      Assert.False(review.Resolve(cancel));
    });
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void Review_DoesNotOverwriteManualEdits(bool sharedEditor)
  {
    RunInSta(() =>
    {
      var editor = new TextEditor { Text = "dog" };
      var markers = CreateMarkers(editor, sharedEditor);
      markers.Service.GetType().GetMethod("AddReplacementMarker", new[] { typeof(int), typeof(int), typeof(string) })!
        .Invoke(markers.Service, new object[] { 0, 3, "cat" });
      var segment = Assert.Single(GetRanges(markers.Service));
      var review = (Ask.UI.Shared.Search.ReplacementReview)segment.GetType().GetProperty("Review")!.GetValue(segment)!;
      editor.Document.Replace(1, 1, "X");
      Assert.False(review.CanCancel);
      Assert.False(review.Resolve(true));
      Assert.Equal("dXg", editor.Text);
      Assert.True(review.Resolve(false));
      Assert.Empty(GetRanges(markers.Service));
    });
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void HoverMenu_OpensOnReplacementAndResolvesThroughMenuItem(bool cancel)
  {
    RunInSta(() =>
    {
      var editor = new TextEditor { Text = "dog", FontSize = 18 };
      var markers = new Ask.UI.Controls.TextEditorControl.TextMarkerService(editor);
      markers.AddReplacementMarker(0, 3, "cat");
      var window = new System.Windows.Window
      {
        Content = editor, Width = 300, Height = 100, ShowActivated = false, ShowInTaskbar = false, Opacity = 0
      };
      try
      {
        window.Show();
        window.UpdateLayout();
        var view = editor.TextArea.TextView;
        view.EnsureVisualLines();
        var segment = Assert.Single(GetRanges(markers));
        var rect = ICSharpCode.AvalonEdit.Rendering.BackgroundGeometryBuilder.GetRectsForSegment(view, segment).First();
        var menu = (Ask.UI.Shared.Search.ReplacementReviewMenu)markers.GetType()
          .GetField("_replacementMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(markers)!;
        Assert.True(menu.TryOpen(new System.Windows.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2)));
        Assert.True(menu.Menu!.IsOpen);
        var item = (System.Windows.Controls.MenuItem)menu.Menu.Items[cancel ? 1 : 0];
        menu.Menu.UpdateLayout();
        item.ApplyTemplate();
        item.UpdateLayout();
        var headerPresenter = (System.Windows.Controls.ContentPresenter)item.Template.FindName("HeaderPresenter", item);
        var textSize = new System.Windows.Media.FormattedText((string)item.Header,
          System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight,
          new System.Windows.Media.Typeface(item.FontFamily, item.FontStyle, item.FontWeight, item.FontStretch),
          item.FontSize, System.Windows.Media.Brushes.Black, System.Windows.Media.VisualTreeHelper.GetDpi(item).PixelsPerDip);
        Assert.True(headerPresenter.ActualWidth >= textSize.WidthIncludingTrailingWhitespace);
        Assert.True(menu.Menu.ActualWidth < 160);

        item.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
        Assert.Equal(cancel ? "cat" : "dog", editor.Text);
        Assert.Empty(GetRanges(markers));
        Assert.Null(menu.Menu);
      }
      finally { window.Close(); }
    });
  }

  private sealed record Harness(object Service, Action<int, int> AddReplacement,
    Action<int, int, bool> AddSearch, Action<bool> Clear, Action ClearReplacements);

  private static Harness CreateMarkers(TextEditor editor, bool shared)
  {
    if (shared)
    {
      var markers = new Ask.UI.Controls.TextEditorControl.TextMarkerService(editor);
      return new(markers, markers.AddReplacementMarker, markers.AddSearchMarker, markers.ClearAllMarkers, markers.ClearReplacementMarkers);
    }
    else
    {
      var markers = new global::UI.Controls.TextEditorControl.TextMarkerService(editor);
      return new(markers, markers.AddReplacementMarker, markers.AddSearchMarker, markers.ClearAllMarkers, markers.ClearReplacementMarkers);
    }
  }

  // Inspect actual AvalonEdit segments without adding a production-only diagnostics API.
  private static TextSegment[] GetRanges(object service) => ((IEnumerable)service.GetType()
    .GetField("markers", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!).Cast<TextSegment>().ToArray();

  private static void RunInSta(Action action)
  {
    Exception? failure = null;
    var thread = new Thread(() => { try { action(); } catch (Exception error) { failure = error; } });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
    if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
  }
}
