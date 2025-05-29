using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TestManyWindows
{
  public class DockingTab : Border
  {
    private TextBlock _title;
    private Button _closeButton;
    public UIElement Content { get; set; }
    private Point _dragStart;

    private bool _isSelected;
    public bool IsSelected
    {
      get => _isSelected;
      set
      {
        _isSelected = value;
        Background = value ? Brushes.DodgerBlue : Brushes.DarkSlateGray;
        _title.Foreground = Brushes.White;
      }
    }

    public DockingTab(string title, Action onClose, Action onSelect)
    {
      CornerRadius = new CornerRadius(8, 8, 0, 0);
      Padding = new Thickness(8, 4, 8, 4);
      Margin = new Thickness(2, 0, 0, 0);
      Background = Brushes.DarkSlateGray;

      var stack = new StackPanel { Orientation = Orientation.Horizontal };
      _title = new TextBlock { Text = title, Foreground = Brushes.White, Margin = new Thickness(0, 0, 4, 0) };
      _closeButton = new Button { Content = "X", Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), Width = 16, Height = 16 };

      _closeButton.Click += (s, e) => onClose();
      MouseLeftButtonDown += (s, e) =>
      {
        _dragStart = e.GetPosition(this);
        onSelect();
        CaptureMouse();
      };

      MouseMove += (s, e) =>
      {
        if (IsMouseCaptured)
        {
          var pos = e.GetPosition(this);
          if (Math.Abs(pos.X - _dragStart.X) > 5 || Math.Abs(pos.Y - _dragStart.Y) > 5)
          {
            ReleaseMouseCapture();
            StartDrag();
          }
        }
      };

      stack.Children.Add(_title);
      stack.Children.Add(_closeButton);
      Child = stack;
    }

    private void StartDrag()
        {
            var newWindow = new DockingWindow();
            newWindow.DockingPanel.AddTab(_title.Text, Content);
            newWindow.Show();

            (Parent as Panel)?.Children.Remove(this);
        }

        public void SetActive(bool isActive)
        {
            Background = isActive ? Brushes.DodgerBlue : Brushes.DarkSlateGray;
            _title.Foreground = Brushes.White;
        }
  }
}
