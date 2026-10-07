using System.Windows;
using System.Windows.Media.Animation;

namespace MainWindowProgram.Init;

/// <summary>Плавно показывает и скрывает окна на их Dispatcher.</summary>
internal static class WindowOpacityTransition
{
  private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(180);

  internal static async Task<bool> ShowAsync(Window window)
  {
    window.Opacity = 0;
    window.Show();
    return await FadeAsync(window, 1);
  }

  internal static async Task HideAsync(Window window)
  {
    if (!window.IsVisible) return;
    if (await FadeAsync(window, 0))
    {
      window.Hide();
      window.Opacity = 1;
    }
  }

  internal static Task<bool> FadeAsync(Window window, double opacity)
  {
    window.Dispatcher.VerifyAccess();
    var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    var animation = new DoubleAnimation(window.Opacity, opacity, Duration)
    {
      EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
      FillBehavior = FillBehavior.Stop
    };
    animation.Completed += AnimationCompleted;
    window.Closed += WindowClosed;
    window.BeginAnimation(UIElement.OpacityProperty, animation);
    return completion.Task;

    void Finish(bool completed)
    {
      animation.Completed -= AnimationCompleted;
      window.Closed -= WindowClosed;
      if (completed) window.Opacity = opacity;
      window.BeginAnimation(UIElement.OpacityProperty, null);
      completion.TrySetResult(completed);
    }
    void AnimationCompleted(object? sender, EventArgs args) => Finish(true);
    void WindowClosed(object? sender, EventArgs args) => Finish(false);
  }
}
