using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UI.Components.SearchControls;
using static Ask.LogLib.LoggerUtility;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using ComboBox = System.Windows.Controls.ComboBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;

namespace UI.Controls.Search
{
  /// <summary>
  /// Окно поиска, содержащее функции поиска и замены, а также возможность сворачивания/разворачивания строки замены.
  /// </summary>
  public partial class SearchWindow : Window
  {
    /// <summary>
    /// Флаг, указывающий, развернута ли строка замены.
    /// </summary>
    private bool _isExpanded = false;

    private const double MinWindowHeight = 90;  // Высота окна без строки 2 (увеличено под более высокую 3ю строку)
    private bool _allowClose;
    private Window _parentWindow;
    private bool IsLoaded;

    public event Action ClearHighlights;

    public event Action SelectFileForSearch;
    public string SearchTextData { get; set; }

    private bool _isDraggingSlider = false;
    private Point _dragStartScreenPoint;
    private double _windowStartLeft;
    private Grid ReplaceRowPanelElement => (Grid)FindName("ReplaceRowPanel");
    private Grid OptionsRowPanelElement => (Grid)FindName("OptionsRowPanel");


    /// <summary>
    /// Высота окна при развернутой строке замены.
    /// </summary>
    private double ExpandedWindowHeight => 130;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SearchWindow"/>.
    /// </summary>
    public SearchWindow()
    {
      InitializeComponent();
      this.Loaded += Window_Loaded;

      EventAggregator.Subscribe<SearchEvents.SearchButtonPressed>(e => OnSearchButtonPressed(e.SearchParameters));
      EventAggregator.Subscribe<SearchEvents.ReplaceWordButtonPressed>(_ => OnReplaceWordButtonPressed());
      EventAggregator.Subscribe<SearchEvents.ReplaceAllWordsButtonPressed>(_ => OnReplaceAllWordsButtonPressed());
      EventAggregator.Subscribe<SearchEvents.CloseSearchWindow>(_ => OnCloseSearchWindowRequested());
      EventAggregator.Subscribe<SearchEvents.SearchTextRequested>(e => OnSearchTextRequested(e.SelectedText));
      this.PreviewKeyDown += SearchWindow_PreviewKeyDown;

      this.Focus();
      SearchTextBox.Focus();
      LogInformation("Окно поиска инициализировано");
    }


    /// <summary>
    /// Обработчик события загрузки окна.
    /// Устанавливает начальную высоту окна и скрывает строку замены.
    /// </summary>
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
      ReplaceRowPanelElement.Visibility = Visibility.Collapsed;
      ReplaceRowPanelElement.Opacity = 0;
      OptionsRowPanelElement.RenderTransform = new TranslateTransform(0, 0);
      UpdateWindowHeight(MinWindowHeight);
      var showAnimation = (Storyboard)Resources["ShowAnimation"];
      showAnimation.Begin();
      LogInformation("Окно поиска загружено.");
    }

    #region Отслеживание родительского окна

    private void ParentWindow_LocationChanged(object sender, EventArgs e)
    {
      UpdatePosition();
      LogInformation("Изменено положение родительского окна");
    }

    private void ParentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      UpdatePosition();
      LogInformation("Изменен размер родительского окна");
    }

    private void ParentWindow_StateChanged(object sender, EventArgs e)
    {
      UpdatePosition();
      LogInformation("Изменен статус родительского окна");
    }

    private const double ToolPanelHeight = 50; // Высота панели инструментов, которую нужно учесть сверху

    private void UpdatePosition()
    {
      if (_parentWindow == null || !this.IsLoaded)
      {
        return;
      }

      // Получаем координаты родительского окна
      Point parentTopLeft = _parentWindow.PointToScreen(new Point(0, 0));
      double parentWidth = _parentWindow.ActualWidth;
      double parentHeight = _parentWindow.ActualHeight;

      // Получаем рабочую область экрана, на котором находится родительское окно
      var parentWindowHandle = new System.Windows.Interop.WindowInteropHelper(_parentWindow).Handle;
      System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(parentWindowHandle);
      System.Drawing.Rectangle workingArea = screen.WorkingArea; // Рабочая область монитора

      // Располагаем окно поиска по центру сверху относительно родительского окна
      double newLeft = parentTopLeft.X + ((parentWidth - this.Width) / 2);  // Центрируем по горизонтали относительно родителя
      double newTop = parentTopLeft.Y + ToolPanelHeight; // Окно поиска привязываем к верхней части родителя

      // Ограничения по горизонтали
      newLeft = Math.Max(workingArea.Left, Math.Min(newLeft, workingArea.Right - this.Width));
      // Ограничения по вертикали
      newTop = Math.Max(workingArea.Top, Math.Min(newTop, workingArea.Bottom - this.Height));

      // Убедитесь, что окно не выходит за пределы экрана
      if (newLeft < workingArea.Left)
      {
        newLeft = workingArea.Left;  // Устанавливаем на левый край экрана
      }
      if (newTop < workingArea.Top)
      {
        newTop = workingArea.Top;  // Устанавливаем на верхний край экрана
      }

      this.Left = newLeft;
      this.Top = newTop;

      LogInformation($"Координаты левого верхнего угла окна поиска: X:{newLeft} Y:{newTop}");
    }

    #endregion

    #region Изменение размеров окна

    private void UpdateWindowHeight(double newHeight)
    {
      this.Height = newHeight;
      this.MinHeight = newHeight;
      this.MaxHeight = newHeight;
    }

    /// <summary>
    /// Обработчик изменения размера окна.
    /// Ограничивает изменение высоты окна.
    /// </summary>
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
    public async Task ShowWindow(bool expandReplaceRow = false, bool focusReplaceField = false)
    {
      if (this.Top < 0)
      {
        this.Top = 50;
      }
      this.Activate();
      this.Focus();
      this.Show();

      var showAnimation = (Storyboard)Resources["ShowAnimation"];
      showAnimation.Begin();

      if (expandReplaceRow || focusReplaceField)
      {
        await SetReplaceRowStateAsync(true, focusReplaceField);
      }

      // Гарантируем фокус на строке поиска после показа окна.
      this.Dispatcher.InvokeAsync(() =>
      {
        this.Focus();
        SearchTextBox.Focus();
        Keyboard.Focus(SearchTextBox);
      }, System.Windows.Threading.DispatcherPriority.Input);
    }

    public void FocusReplaceField() => FocusReplaceTextBox();

    public void CloseDialog()
    {
      ClearHighlights?.Invoke();
      SearchEventAdapter.RaiseSearchWindowClosing(false);
      _allowClose = true;
      var hideAnimation = (Storyboard)Resources["HideAnimation"];
      hideAnimation.Completed += (s, e) => this.Hide();
      hideAnimation.Begin();
    }

    private async void ToggleArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await SetReplaceRowStateAsync(!_isExpanded);
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

    private async Task SetReplaceRowStateAsync(bool expand, bool focusReplaceField = false)
    {
      if (_isExpanded == expand)
      {
        if (expand && focusReplaceField)
        {
          FocusReplaceTextBox();
        }
        return;
      }

      _isExpanded = expand;
      ToggleArrow.IsArrowUp = !expand;

      if (expand)
      {
        UpdateWindowHeight(ExpandedWindowHeight);
        ReplaceRow.Height = new GridLength(1, GridUnitType.Star);
        ReplaceRowPanelElement.Visibility = Visibility.Visible;
        await Task.WhenAll(
          PlayStoryboardAsync("ExpandReplaceRowAnimation"),
          PlayStoryboardAsync("OptionsShiftDownAnimation"));
        OptionsRowPanelElement.RenderTransform = new TranslateTransform(0, 0);
        if (focusReplaceField)
        {
          FocusReplaceTextBox();
        }
      }
      else
      {
        await Task.WhenAll(
          PlayStoryboardAsync("CollapseReplaceRowAnimation"),
          PlayStoryboardAsync("OptionsShiftUpAnimation"));
        ReplaceRow.Height = new GridLength(0);
        ReplaceRowPanelElement.Visibility = Visibility.Collapsed;
        OptionsRowPanelElement.RenderTransform = new TranslateTransform(0, 0);
        UpdateWindowHeight(MinWindowHeight);
      }
    }

    private void FocusReplaceTextBox()
    {
      ReplaceTextBox.Focus();
      ReplaceTextBox.CaretIndex = ReplaceTextBox.Text.Length;
      ReplcePlaceholder.Visibility = Visibility.Collapsed;
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
      SendSearchRequest(searchParameters);
    }

    private void SendSearchRequest(string searchParameters)
    {
      var searchText = SearchTextBox.Text;
      var searchArea = searchAreaParameters.SelectedIndex;
      var wholeWord = wholeWordButton.IsChecked;
      var caseWord = caseButton.IsChecked;
      if (searchArea == 2)
      {
        searchAreaParameters.SelectedIndex = 0;
        searchArea = 0;
      }

      SearchEventAdapter.RaiseSearchText(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    private void OnReplaceWordButtonPressed()
    {
      var searchText = SearchTextBox.Text;
      var replaceText = ReplaceTextBox.Text;
      var searchArea = searchAreaParameters.SelectedIndex;
      var wholeWord = wholeWordButton.IsChecked;
      var caseWord = caseButton.IsChecked;
      if (searchArea == 2)
      {
        searchAreaParameters.SelectedIndex = 0;
      }

      SearchEventAdapter.RaiseReplaceText(replaceText, searchText, wholeWord, caseWord, searchArea, "FindNext");
    }

    private void OnReplaceAllWordsButtonPressed()
    {
      var searchText = SearchTextBox.Text;
      var replaceText = ReplaceTextBox.Text;
      var searchArea = searchAreaParameters.SelectedIndex;
      var wholeWord = wholeWordButton.IsChecked;
      var caseWord = caseButton.IsChecked;
      if (searchArea == 2)
      {
        searchAreaParameters.SelectedIndex = 0;
      }

      SearchEventAdapter.RaiseReplaceText(replaceText, searchText, wholeWord, caseWord, searchArea, "FindAll");
    }

    private void OnCloseSearchWindowRequested()
    {
      CloseDialog();
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

    private void OnSearchTextRequested(string selectedText)
    {
      if (!string.IsNullOrEmpty(selectedText))
      {
        SearchTextBox.Text = selectedText;
        SearchTextBox.Focus();
        SearchPlaceholder.Visibility = Visibility.Collapsed;
        SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
      }
    }

    private void SearchWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        e.Handled = true;
        CloseDialog();
        return;
      }

      if (e.Key == Key.F3)
      {
        if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
          e.Handled = true;
          SendSearchRequest("FindPrevious");
        }
        else if (Keyboard.Modifiers == ModifierKeys.None)
        {
          e.Handled = true;
          SendSearchRequest("FindNext");
        }
      }
    }

    private void SearchSlider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      StopAnimations();

      _isDraggingSlider = true;

      // Получаем координаты мыши в пределах экрана
      _dragStartScreenPoint = PointToScreen(e.GetPosition(this));
      _windowStartLeft = this.Left;

      SearchSlider.CaptureMouse();
    }

    private void SearchSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      _isDraggingSlider = false;
      SearchSlider.ReleaseMouseCapture();
    }

    private void SearchSlider_MouseMove(object sender, MouseEventArgs e)
    {
      if (_isDraggingSlider && e.LeftButton == MouseButtonState.Pressed && _parentWindow != null)
      {
        Point currentScreenPoint = PointToScreen(e.GetPosition(this));
        double deltaX = currentScreenPoint.X - _dragStartScreenPoint.X;

        double newLeft = _windowStartLeft + deltaX;

        // Получаем границы окна-родителя
        var ownerTopLeft = _parentWindow.PointToScreen(new Point(0, 0));
        double ownerLeft = ownerTopLeft.X;
        double ownerRight = ownerLeft + _parentWindow.ActualWidth;

        double windowWidth = this.ActualWidth;

        // Ограничение по левому краю
        if (newLeft < ownerLeft)
        {
          newLeft = ownerLeft;
        }

        // Ограничение по правому краю
        if (newLeft + windowWidth > ownerRight)
        {
          newLeft = ownerRight - windowWidth - 15;
        }

        this.Left = newLeft;
      }
    }

    private void StopAnimations()
    {
      if (Resources["ShowAnimation"] is Storyboard showAnimation)
        showAnimation.Stop();

      if (Resources["HideAnimation"] is Storyboard hideAnimation)
        hideAnimation.Stop();

      var resetGroup = new TransformGroup();
      resetGroup.Children.Add(new ScaleTransform(1, 1));
      resetGroup.Children.Add(new TranslateTransform(0, 0));
      WindowContainer.RenderTransform = resetGroup; // Сброс
      WindowContainer.Opacity = 1;
    }

    private Task PlayStoryboardAsync(string storyboardResourceKey)
    {
      if (Resources[storyboardResourceKey] is Storyboard baseStoryboard)
      {
        var storyboard = baseStoryboard.Clone();
        var tcs = new TaskCompletionSource<bool>();

        void OnCompleted(object sender, EventArgs args)
        {
          storyboard.Completed -= OnCompleted;
          tcs.TrySetResult(true);
        }

        storyboard.Completed += OnCompleted;
        storyboard.Begin(this, true);
        return tcs.Task;
      }

      return Task.CompletedTask;
    }
  }
}
