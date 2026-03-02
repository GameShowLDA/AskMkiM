using Ask.UI.Features.Notifications.Models;
using Ask.UI.Features.Notifications.ViewModels;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ask.UI.Features.Notifications.Views
{
  public partial class WindowNotificationManagerControl : UserControl
  {
    private readonly Dictionary<NotificationItemViewModel, PropertyChangedEventHandler> _itemHandlers = new();
    private readonly HashSet<NotificationItemViewModel> _closingItems = new();

    public WindowNotificationManagerControl()
    {
      InitializeComponent();
      DataContext = NotificationHostService.Instance.ViewModel;
    }

    private void OnNotificationCardMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is FrameworkElement { DataContext: NotificationItemViewModel item })
      {
        NotificationHostService.Instance.Dismiss(item);
        e.Handled = true;
      }
    }

    private void OnNotificationCardLoaded(object sender, RoutedEventArgs e)
    {
      if (sender is not Border card || card.DataContext is not NotificationItemViewModel item)
      {
        return;
      }

      if (!_itemHandlers.ContainsKey(item))
      {
        PropertyChangedEventHandler handler = (_, args) =>
        {
          if (args.PropertyName == nameof(NotificationItemViewModel.IsClosing) && item.IsClosing)
          {
            _ = Dispatcher.InvokeAsync(() => BeginCloseAnimation(card, item));
          }
        };

        _itemHandlers[item] = handler;
        item.PropertyChanged += handler;
      }

      BeginOpenAnimation(card);

      if (item.IsClosing)
      {
        BeginCloseAnimation(card, item);
      }
    }

    private void OnNotificationCardUnloaded(object sender, RoutedEventArgs e)
    {
      if (sender is not Border card || card.DataContext is not NotificationItemViewModel item)
      {
        return;
      }

      if (_itemHandlers.Remove(item, out var handler))
      {
        item.PropertyChanged -= handler;
      }

      _closingItems.Remove(item);
      ResetCardState(card);
    }

    private void BeginOpenAnimation(Border card)
    {
      if (!TryGetTransforms(card, out var scaleTransform, out var translateTransform))
      {
        return;
      }

      scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
      scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
      translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
      translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
      card.BeginAnimation(OpacityProperty, null);

      scaleTransform.ScaleX = 0.85;
      scaleTransform.ScaleY = 0.85;
      translateTransform.X = 0;
      translateTransform.Y = 20;
      card.Opacity = 0;

      translateTransform.BeginAnimation(TranslateTransform.YProperty, CreateOpenTranslateAnimation());
      scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, CreateOpenScaleAnimation());
      scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, CreateOpenScaleAnimation());
      card.BeginAnimation(OpacityProperty, new DoubleAnimation
      {
        To = 1,
        Duration = TimeSpan.FromMilliseconds(450),
        FillBehavior = FillBehavior.HoldEnd,
      });
    }

    private void BeginCloseAnimation(Border card, NotificationItemViewModel item)
    {
      if (!_closingItems.Add(item))
      {
        return;
      }

      if (!TryGetTransforms(card, out var scaleTransform, out var translateTransform))
      {
        return;
      }

      translateTransform.BeginAnimation(TranslateTransform.XProperty, CreateCloseTranslateXAnimation());
      scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, CreateCloseScaleYAnimation());
      card.BeginAnimation(OpacityProperty, new DoubleAnimation
      {
        BeginTime = TimeSpan.FromMilliseconds(450),
        Duration = TimeSpan.FromMilliseconds(800),
        To = 0,
        FillBehavior = FillBehavior.HoldEnd,
      });
    }

    private static DoubleAnimationUsingKeyFrames CreateOpenTranslateAnimation()
    {
      var animation = new DoubleAnimationUsingKeyFrames
      {
        Duration = TimeSpan.FromMilliseconds(450),
        FillBehavior = FillBehavior.HoldEnd,
      };

      animation.KeyFrames.Add(new EasingDoubleKeyFrame(20, KeyTime.FromTimeSpan(TimeSpan.Zero)));
      animation.KeyFrames.Add(new EasingDoubleKeyFrame(-20, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(135)))
      {
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
      });
      animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(450))));

      return animation;
    }

    private static DoubleAnimationUsingKeyFrames CreateOpenScaleAnimation()
    {
      var animation = new DoubleAnimationUsingKeyFrames
      {
        Duration = TimeSpan.FromMilliseconds(450),
        FillBehavior = FillBehavior.HoldEnd,
      };

      animation.KeyFrames.Add(new EasingDoubleKeyFrame(0.85, KeyTime.FromTimeSpan(TimeSpan.Zero)));
      animation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(450))));

      return animation;
    }

    private static DoubleAnimationUsingKeyFrames CreateCloseTranslateXAnimation()
    {
      var animation = new DoubleAnimationUsingKeyFrames
      {
        Duration = TimeSpan.FromMilliseconds(1250),
        FillBehavior = FillBehavior.HoldEnd,
      };

      animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
      animation.KeyFrames.Add(new EasingDoubleKeyFrame(800, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(875)))
      {
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
      });
      animation.KeyFrames.Add(new EasingDoubleKeyFrame(800, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1250))));

      return animation;
    }

    private static DoubleAnimationUsingKeyFrames CreateCloseScaleYAnimation()
    {
      var animation = new DoubleAnimationUsingKeyFrames
      {
        Duration = TimeSpan.FromMilliseconds(1250),
        FillBehavior = FillBehavior.HoldEnd,
      };

      animation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
      animation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(875))));
      animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1250))));

      return animation;
    }

    private static bool TryGetTransforms(Border card, out ScaleTransform scaleTransform, out TranslateTransform translateTransform)
    {
      scaleTransform = null!;
      translateTransform = null!;

      if (card.RenderTransform is not TransformGroup { Children.Count: >= 2 } transformGroup)
      {
        return false;
      }

      if (transformGroup.Children[0] is not ScaleTransform scale ||
          transformGroup.Children[1] is not TranslateTransform translate)
      {
        return false;
      }

      scaleTransform = scale;
      translateTransform = translate;
      return true;
    }

    private static void ResetCardState(Border card)
    {
      if (!TryGetTransforms(card, out var scaleTransform, out var translateTransform))
      {
        return;
      }

      scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
      scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
      translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
      translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
      card.BeginAnimation(OpacityProperty, null);
    }
  }

  public sealed class StringHasTextToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return string.IsNullOrWhiteSpace(value as string)
        ? Visibility.Collapsed
        : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return Binding.DoNothing;
    }
  }

  public sealed class NotificationTypeToGlyphConverter : IValueConverter
  {
    private static readonly Geometry Info = Geometry.Parse("M11,9H13V7H11M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20M11,17H13V11H11V17Z");
    private static readonly Geometry Success = Geometry.Parse("M12,2A10,10 0 1,0 22,12A10,10 0 0,0 12,2M9.5,16.2L5.8,12.5L7.2,11.1L9.5,13.4L16.8,6.1L18.2,7.5L9.5,16.2Z");
    private static readonly Geometry Warning = Geometry.Parse("M1,21H23L12,2L1,21M13,18H11V16H13V18M13,14H11V10H13V14Z");
    private static readonly Geometry Error = Geometry.Parse("M12,2A10,10 0 1,0 22,12A10,10 0 0,0 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var type = value is NotificationType notificationType
        ? notificationType
        : NotificationType.Information;

      return type switch
      {
        NotificationType.Success => Success,
        NotificationType.Warning => Warning,
        NotificationType.Error => Error,
        _ => Info,
      };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return Binding.DoNothing;
    }
  }

}
