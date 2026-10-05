using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UI.Controls.Calendar;

namespace Ask.UI.UnitTests.Services.Calendar;

public sealed class TaskTitleTests
{
  [Fact]
  public void CompletedWrappedTitle_AddsStrikeOnEachLine_AndUndoRestoresText()
  {
    RunInSta(() =>
    {
      var title = new TaskTitle
      {
        Text = "Проверить оборудование и подготовить установку",
        FontFamily = new FontFamily("Segoe UI"), FontSize = 16,
        Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, Width = 160
      };
      title.Measure(new Size(160, 200));
      title.Arrange(new Rect(0, 0, 160, title.DesiredSize.Height));
      title.UpdateLayout();
      var original = Render(title);
      title.IsCompleted = true;
      var completed = Render(title);
      int stride = 160 * 4;
      int struckRows = Enumerable.Range(0, original.Length / stride).Count(row =>
        Enumerable.Range(0, 160).Count(column =>
          completed[row * stride + column * 4 + 3] > original[row * stride + column * 4 + 3] + 20) > 40);
      Assert.True(struckRows >= 2, "Зачёркивание должно появляться на каждой строке заголовка.");
      title.IsCompleted = false;
      Assert.Equal(original, Render(title));
    });
  }

  [Fact]
  public void AlreadyCompletedTitle_LoadsWithoutAnimation()
  {
    RunInSta(() =>
    {
      var title = new TaskTitle { Text = "Готово", IsCompleted = true };
      title.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
      Assert.True(title.IsCompleted);
      Assert.False(title.HasAnimatedProperties);
    });
  }

  private static byte[] Render(TaskTitle title)
  {
    title.UpdateLayout();
    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
    var bitmap = new RenderTargetBitmap(160, (int)Math.Ceiling(title.ActualHeight), 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(title);
    var pixels = new byte[160 * bitmap.PixelHeight * 4];
    bitmap.CopyPixels(pixels, 160 * 4, 0);
    return pixels;
  }

  private static void RunInSta(Action action)
  {
    Exception? exception = null;
    var thread = new Thread(() => { try { action(); } catch (Exception error) { exception = error; } });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (exception is not null) throw new InvalidOperationException(exception.ToString(), exception);
  }
}
