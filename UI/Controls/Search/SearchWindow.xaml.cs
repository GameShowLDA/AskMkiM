using AppConfig;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using UI.Components.SearchControls;
using UI.Controls.TextEditor;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;

namespace UI.Controls.Search
{
  public partial class SearchWindow : Window
  {
    private bool _isExpanded = false; // По умолчанию строка скрыта

    private const double MinWindowHeight = 80;  // Высота окна без строки 2
    private const double ExpandedWindowHeight = 120; // Высота окна со строкой 2
    private bool _allowClose;
    private Window _parentWindow;
    private bool IsLoaded;
    public event Action<string, bool?, bool?, int, string> SearchText;
    public event Action ClearHighlights;
    public event Action SelectFileForSearch;

    public SearchWindow()
    {
      InitializeComponent();
      this.Loaded += Window_Loaded;
      EventAggregator.SearchButtonPressed += OnSearchButtonPressed;
      EventAggregator.TextEditorClosing += OnSearchWindowClosing;
      EventAggregator.ActiveEditorChanged += OnActiveEditorChanged;
      EventAggregator.SearchTextRequested += OnSearchTextRequested;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      _parentWindow = Window.GetWindow(this)?.Owner;
      if (_parentWindow != null)
      {
        _parentWindow.LocationChanged += ParentWindow_LocationChanged;
        _parentWindow.SizeChanged += ParentWindow_SizeChanged;
        _parentWindow.StateChanged += ParentWindow_StateChanged;
        UpdatePosition();
      }
      this.IsLoaded = true;
      ReplaceRow.Height = new GridLength(0);
      UpdateWindowHeight(MinWindowHeight);
    }

    #region Отслеживание родительского окна

    private void ParentWindow_LocationChanged(object sender, EventArgs e)
    {
      UpdatePosition();
    }

    private void ParentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      UpdatePosition();
    }
    private void ParentWindow_StateChanged(object sender, EventArgs e)
    {
      UpdatePosition();
    }

    private void UpdatePosition()
    {
      if (_parentWindow == null || !this.IsLoaded)
        return;

      var parentWindowHandle = new System.Windows.Interop.WindowInteropHelper(_parentWindow).Handle;
      Screen screen = Screen.FromHandle(parentWindowHandle);
      System.Drawing.Rectangle workingArea = screen.WorkingArea; // Рабочая область монитора

      Point parentRightTop;
      if (_parentWindow.WindowState == WindowState.Maximized)
      {
        parentRightTop = new Point(workingArea.Right, workingArea.Top);
      }
      else
      {
        parentRightTop = _parentWindow.PointToScreen(new Point(_parentWindow.ActualWidth - 15, 0));
      }

      double newLeft = parentRightTop.X - this.Width; // Правый край совпадает с правым краем родителя
      double newTop = parentRightTop.Y + 60; // Отступ сверху

      newLeft = Math.Max(workingArea.Left, Math.Min(newLeft, workingArea.Right - this.Width)); // Ограничение по горизонтали
      newTop = Math.Max(workingArea.Top, Math.Min(newTop, workingArea.Bottom - this.Height)); // Ограничение по вертикали

      this.Left = newLeft;
      this.Top = newTop;
    }

    #endregion


    #region Изменение размеров окна

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

    #endregion


    #region Обработчики событий окна

    private void exitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CloseDialog();
    }
    public void ShowWindow()
    {
      if (!this.IsVisible)
      {
        this.Show();
      }
      this.Activate();
      this.Focus();
    }
    public void CloseDialog()
    {
      ClearHighlights?.Invoke();
      EventAggregator.RaiseSearchWindowClosing(false);
      _allowClose = true;
      this.Hide();
    }

    private void OnCaseChanged(object sender, EventArgs e)
    {
      var button = sender as CaseToggleButton;
      if (button != null)
      {
        MessageBox.Show($"Кнопка включена: {button.IsChecked}");
      }
    }

    private async void ToggleArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      _isExpanded = !_isExpanded;

      ToggleArrow.IsArrowUp = !_isExpanded;

      await Task.Delay(250);
      ReplaceRow.Height = _isExpanded ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

      UpdateWindowHeight(_isExpanded ? ExpandedWindowHeight : MinWindowHeight);
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

    private void searchArrows_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is SearchArrows searchArrows)
      {
        var comboBox = FindChild<ComboBox>(searchArrows);

        if (comboBox != null)
        {
          comboBox.Focus();
          comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
          e.Handled = true;
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
    #endregion
    private void OnSearchButtonPressed(string searchParameters)
    {
      var searchText = SearchTextBox.Text;
      var searchArea = searchAreaParameters.SelectedIndex;
      var wholeWord = wholeWordButton.IsChecked;
      var caseWord = caseButton.IsChecked;
      if (searchArea == 2)
      {
        searchAreaParameters.SelectedIndex = 0;
      }

      SearchText?.Invoke(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    private void OnSearchWindowClosing(bool isOpen)
    {
      if (isOpen)
      {
        CloseDialog();
        EventAggregator.SearchTextRequested -= OnSearchTextRequested;
      }
    }

    private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
      SearchPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
      {
        SearchPlaceholder.Visibility = Visibility.Visible;
      }
    }

    private void ReplaceTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
      ReplcePlaceholder.Visibility = Visibility.Collapsed;
    }

    private void ReplaceTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
      {
        ReplcePlaceholder.Visibility = Visibility.Visible;
      }
    }

    private void PART_EditableTextBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
      var selected = searchAreaParameters.SelectedValue as ComboBoxItem;
      if (selected.Tag.ToString() == "FindInFile")
      {
        SelectFileForSearch?.Invoke();
      }
    }

    private void OnActiveEditorChanged(bool isTextEditor)
    {
      if (isTextEditor)
      {
        if (!this.IsVisible)
        {
          this.Show();
          EventAggregator.RaiseSearchWindowActivated(true);
        }
        this.Activate(); 
      }
      else
      {
        this.Hide();
      }
    }

    private void OnSearchTextRequested(string selectedText)
    {
      if (!string.IsNullOrEmpty(selectedText))
      {
        SearchTextBox.Text = selectedText;
        SearchTextBox.Focus();
      }
    }
  }
}
