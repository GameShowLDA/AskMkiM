using Ask.Core.Services.Errors.Models;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ask.UI.Controls.ErrorList
{
  public partial class IssueTableControl : UserControl
  {
    public static readonly DependencyProperty ItemsSourceProperty =
      DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(IssueTableControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedIssueProperty =
      DependencyProperty.Register(
        nameof(SelectedIssue),
        typeof(IDisplayIssue),
        typeof(IssueTableControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty LineColumnWidthProperty =
      DependencyProperty.Register(
        nameof(LineColumnWidth),
        typeof(GridLength),
        typeof(IssueTableControl),
        new PropertyMetadata(new GridLength(60)));

    public static readonly DependencyProperty MeasureColumnWidthProperty =
      DependencyProperty.Register(
        nameof(MeasureColumnWidth),
        typeof(GridLength),
        typeof(IssueTableControl),
        new PropertyMetadata(new GridLength(0)));

    public static readonly DependencyProperty DebugColumnWidthProperty =
      DependencyProperty.Register(
        nameof(DebugColumnWidth),
        typeof(GridLength),
        typeof(IssueTableControl),
        new PropertyMetadata(new GridLength(0)));

    public IssueTableControl()
    {
      InitializeComponent();
    }

    public event Action<IDisplayIssue>? ItemDoubleClicked;

    public IEnumerable? ItemsSource
    {
      get => (IEnumerable?)GetValue(ItemsSourceProperty);
      set => SetValue(ItemsSourceProperty, value);
    }

    public IDisplayIssue? SelectedIssue
    {
      get => (IDisplayIssue?)GetValue(SelectedIssueProperty);
      set => SetValue(SelectedIssueProperty, value);
    }

    public GridLength LineColumnWidth
    {
      get => (GridLength)GetValue(LineColumnWidthProperty);
      set => SetValue(LineColumnWidthProperty, value);
    }

    public GridLength MeasureColumnWidth
    {
      get => (GridLength)GetValue(MeasureColumnWidthProperty);
      set => SetValue(MeasureColumnWidthProperty, value);
    }

    public GridLength DebugColumnWidth
    {
      get => (GridLength)GetValue(DebugColumnWidthProperty);
      set => SetValue(DebugColumnWidthProperty, value);
    }

    public Visibility StringsNumberVisible
    {
      get => LineColumnWidth.Value > 0 ? Visibility.Visible : Visibility.Collapsed;
      set => LineColumnWidth = value == Visibility.Visible
        ? new GridLength(60)
        : new GridLength(0);
    }

    public Visibility MeasureResultVisible
    {
      get => MeasureColumnWidth.Value > 0 ? Visibility.Visible : Visibility.Collapsed;
      set => MeasureColumnWidth = value == Visibility.Visible
        ? new GridLength(150)
        : new GridLength(0);
    }

    public Visibility DebugVisible
    {
      get => DebugColumnWidth.Value > 0 ? Visibility.Visible : Visibility.Collapsed;
      set => DebugColumnWidth = value == Visibility.Visible
        ? new GridLength(1, GridUnitType.Star)
        : new GridLength(0);
    }

    private void IssueRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if ((sender as FrameworkElement)?.DataContext is not IDisplayIssue issue)
        return;

      SelectedIssue = issue;

      if (e.ClickCount == 2)
      {
        ItemDoubleClicked?.Invoke(issue);
        e.Handled = true;
      }
    }
  }
}
