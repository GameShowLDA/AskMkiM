using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UI.Components.SearchControls;
using Brush = System.Windows.Media.Brush;

namespace UI.Controls.Search
{
  public partial class SearchWindow : Window
  {
    private bool _isExpanded = false; // По умолчанию строка скрыта

    private const double MinWindowHeight = 80;  // Высота окна без строки 2
    private const double ExpandedWindowHeight = 120; // Высота окна со строкой 2
    private bool _allowClose;
    public SearchWindow()
    {
      InitializeComponent();
      DefaultGotAndLostEvent(SearchTextBox, SearchTextBox.Tag.ToString());
      DefaultGotAndLostEvent(ReplaceTextBox, ReplaceTextBox.Tag.ToString());
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      ReplaceRow.Height = new GridLength(0);
      UpdateWindowHeight(MinWindowHeight);
    }

    private async void ToggleArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      _isExpanded = !_isExpanded;


      ToggleArrow.IsArrowUp = !_isExpanded;

      await Task.Delay(250);
      ReplaceRow.Height = _isExpanded ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

      UpdateWindowHeight(_isExpanded ? ExpandedWindowHeight : MinWindowHeight);
    }

    private void UpdateWindowHeight(double newHeight)
    {
      this.Height = newHeight;
      this.MinHeight = newHeight;
      this.MaxHeight = newHeight;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      this.Height = this.MinHeight;
    }

    private void OnCaseChanged(object sender, EventArgs e)
    {
      var button = sender as CaseToggleButton;
      if (button != null)
      {
        MessageBox.Show($"Кнопка включена: {button.IsChecked}");
      }
    }
    private void PART_EditableTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is TextBox textBox && textBox.TemplatedParent is ComboBox comboBox)
      {
        comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
      }
    }

    private void ToggleButton_MouseEnter(object sender, MouseEventArgs e)
    {
      var toggleButton = (ToggleButton)sender;
      var parent = (FrameworkElement)toggleButton.Parent;
      var textBox = parent.FindName("PART_EditableTextBox") as TextBox;

      var color = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];

      toggleButton.Foreground = color;
      if (textBox != null)
        textBox.Foreground = color;
    }
    private void ToggleButton_MouseLeave(object sender, MouseEventArgs e)
    {
      var toggleButton = (ToggleButton)sender;
      var parent = (FrameworkElement)toggleButton.Parent;
      var textBox = parent.FindName("PART_EditableTextBox") as TextBox;

      var color = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];

      toggleButton.Foreground = color;
      if (textBox != null)
        textBox.Foreground = color;
    }

    private void PART_EditableTextBox_MouseEnter(object sender, MouseEventArgs e)
    {
      var textBox = (TextBox)sender;
      var parent = (FrameworkElement)textBox.Parent;
      var toggleButton = parent.FindName("ComboBoxToggleButton") as ToggleButton;

      var color = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];

      textBox.Foreground = color;
      if (toggleButton != null)
        toggleButton.Foreground = color;
    }

    private void PART_EditableTextBox_MouseLeave(object sender, MouseEventArgs e)
    {
      var textBox = (TextBox)sender;
      var parent = (FrameworkElement)textBox.Parent;
      var toggleButton = parent.FindName("ComboBoxToggleButton") as ToggleButton;

      var color = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];

      textBox.Foreground = color;
      if (toggleButton != null)
        toggleButton.Foreground = color;
    }

    /// <summary>
    /// Настраивает события GotFocus и LostFocus для TextBox.
    /// </summary>
    /// <param name="textBox">TextBox для настройки.</param>
    /// <param name="defaultText">Текст по умолчанию для TextBox.</param>
    static public void DefaultGotAndLostEvent(TextBox textBox, string defaultText)
    {
      textBox.GotFocus += (sender, e) =>
      {
        if (textBox.Text == defaultText)
        {
          textBox.Text = string.Empty;
        }
      };

      textBox.LostFocus += (sender, e) =>
      {
        if (string.IsNullOrEmpty(textBox.Text))
        {
          textBox.Text = defaultText;
        }
      };
    }


    private void exitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CloseDialog();
    }
    public void CloseDialog()
    {
      _allowClose = true;
      this.Close();
    }

    private void searchArrows_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is SearchArrows searchArrows)
      {
        var comboBox = FindChild<ComboBox>(searchArrows);

        if (comboBox != null)
        {
          comboBox.Focus();
          comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
          e.Handled = true; // Предотвращаем дальнейшую обработку события
        }
      }
    }

    private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is T typedChild)
        {
          return typedChild;
        }
        var result = FindChild<T>(child);
        if (result != null)
        {
          return result;
        }
      }
      return null;
    }

    private void searchArrows_MouseEnter(object sender, MouseEventArgs e)
    {
      var searchArrows = (SearchArrows)sender;
      var parent = (FrameworkElement)searchArrows.Parent;
      var toggleButton = searchArrows.FindName("ComboBoxToggleButton") as ToggleButton;
      var color = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];

      searchArrows.Foreground = color; 
      if (toggleButton != null)
        toggleButton.Foreground = color;
    }

    private void searchArrows_MouseLeave(object sender, MouseEventArgs e)
    {
      var searchArrows = (SearchArrows)sender;
      var parent = (FrameworkElement)searchArrows.Parent;
      var toggleButton = parent.FindName("ComboBoxToggleButton") as ToggleButton;
      var color = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];

      searchArrows.Foreground = color;
      if (toggleButton != null)
        toggleButton.Foreground = color;
    }
  }
}
