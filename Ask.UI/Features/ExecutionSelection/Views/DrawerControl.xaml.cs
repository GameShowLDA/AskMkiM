using Ask.UI.Features.ExecutionSelection.ViewModels;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ask.UI.Features.ExecutionSelection.Views
{
  public partial class DrawerControl : UserControl
  {
    private DrawerViewModel ViewModel => (DrawerViewModel)DataContext;

    public DrawerControl()
    {
      InitializeComponent();
      DataContext = DrawerHostService.Instance.ViewModel;
      DrawerHostService.Instance.EnsureInitialized();
      ViewModel.PropertyChanged += OnViewModelPropertyChanged;
      PreviewKeyDown += OnPreviewKeyDown;
      PreviewLostKeyboardFocus += OnPreviewLostKeyboardFocus;
      Unloaded += (_, _) => ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (ViewModel.IsCustomContent)
      {
        return;
      }

      ViewModel.ConfirmSelection();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        if (!ViewModel.IsCustomContent)
        {
          ViewModel.ConfirmSelection();
        }

        e.Handled = true;
      }
      else if (e.Key == Key.F4)
      {
        ViewModel.Cancel();
        e.Handled = true;
      }
    }

    private void OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      if (!ViewModel.IsOpen)
      {
        return;
      }

      // ComboBox dropdowns are hosted in a separate popup visual tree.
      // While one is open, allowing temporary focus leave prevents immediate popup close.
      if (HasOpenComboBoxDropDown())
      {
        return;
      }

      if (e.NewFocus is DependencyObject newFocus && IsFocusInsideDrawer(newFocus))
      {
        return;
      }

      e.Handled = true;
      Dispatcher.InvokeAsync(EnsureDrawerFocus, System.Windows.Threading.DispatcherPriority.Input);
    }

    private bool HasOpenComboBoxDropDown()
    {
      return FindVisualChildren<ComboBox>(this).Any(comboBox => comboBox.IsDropDownOpen);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
      if (root == null)
      {
        yield break;
      }

      int childrenCount = VisualTreeHelper.GetChildrenCount(root);
      for (int i = 0; i < childrenCount; i++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(root, i);
        if (child is T typedChild)
        {
          yield return typedChild;
        }

        foreach (var nested in FindVisualChildren<T>(child))
        {
          yield return nested;
        }
      }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != nameof(DrawerViewModel.IsOpen))
      {
        return;
      }

      if (ViewModel.IsOpen)
      {
        StartOpenAnimation();
      }
      else
      {
        ResetVisualState();
      }
    }

    private void StartOpenAnimation()
    {
      PanelTranslate.X = Panel.Width;
      Backdrop.Opacity = 0;

      PanelTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, new DoubleAnimation
      {
        From = Panel.Width,
        To = 0,
        Duration = TimeSpan.FromMilliseconds(220),
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
      });

      Backdrop.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
      {
        From = 0,
        To = 1,
        Duration = TimeSpan.FromMilliseconds(180),
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
      });

      Dispatcher.InvokeAsync(EnsureDrawerFocus, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void ResetVisualState()
    {
      PanelTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, null);
      Backdrop.BeginAnimation(UIElement.OpacityProperty, null);
      PanelTranslate.X = Panel.Width;
      Backdrop.Opacity = 0;
    }

    private void EnsureDrawerFocus()
    {
      if (!ViewModel.IsOpen)
      {
        return;
      }

      if (!ViewModel.IsCustomContent)
      {
        CommandsList.Focus();
        Keyboard.Focus(CommandsList);
        return;
      }

      Focus();
      Keyboard.Focus(this);
      MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    private static bool IsFocusInsideDrawer(DependencyObject target)
    {
      DependencyObject? current = target;
      while (current != null)
      {
        if (current is DrawerControl)
        {
          return true;
        }

        current = VisualTreeHelper.GetParent(current);
      }

      return false;
    }
  }
}

