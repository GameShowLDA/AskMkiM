using Ask.Core.Services.EventCore.Adapters;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static Ask.LogLib.LoggerUtility;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для SearchArrows.xaml.
  /// </summary>
  public partial class SearchArrows : UserControl
  {
    /// <summary>
    /// Конструктор для создания экземпляра класса SearchArrows.
    /// Инициализирует компоненты и вызывает метод для инициализации стрелок.
    /// </summary>
    public SearchArrows()
    {
      InitializeComponent();
      InitializeArrows();
    }

    private void InitializeArrows()
    {
      var arrows = new List<ArrowItem>
      {
        new ArrowItem { Name = "FindNext", Description = "Найти далее", GeometryData = Geometry.Parse("M 2 10 L 18 10 M 14 6 L 18 10 L 14 14") },
        new ArrowItem { Name = "FindPrevious", Description = "Найти предыдущий", GeometryData = Geometry.Parse("M 18 10 L 2 10 M 6 6 L 2 10 L 6 14") },
        new ArrowItem { Name = "FindAll", Description = "Найти все", GeometryData = Geometry.Parse("M 6 5 A 5 5 0 1 1 4.99 5.5 Z M 11 12 L 16 16.3") },
      };

      searchArrowsComboBox.ItemsSource = arrows;
    }

    private void ComboBoxToggleButton_MouseEnter(object sender, MouseEventArgs e)
    {
      var toggleButton = sender as ToggleButton;

      var activeColor = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      if (toggleButton != null)
      {
        toggleButton.Foreground = activeColor;
      }
    }

    private void ComboBoxToggleButton_MouseLeave(object sender, MouseEventArgs e)
    {
      var toggleButton = sender as ToggleButton;

      var defaultColor = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];
      if (toggleButton != null)
      {
        toggleButton.Foreground = defaultColor;
      }
    }

    private void PART_ContentPresenter_MouseEnter(object sender, MouseEventArgs e)
    {
      var presenter = (ContentPresenter)sender;
      var path = FindChild<Path>(presenter);

      var activeColor = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      if (path != null)
      {
        path.Stroke = activeColor;
      }
    }

    private void PART_ContentPresenter_MouseLeave(object sender, MouseEventArgs e)
    {
      var presenter = (ContentPresenter)sender;
      var path = FindChild<Path>(presenter);

      var defaultColor = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];
      if (path != null)
      {
        path.Stroke = defaultColor;
      }
    }

    private void ComboBoxBorder_MouseEnter(object sender, MouseEventArgs e)
    {
      var border = (Border)sender;
      var path = FindChild<Path>(border);

      var activeColor = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      if (path != null)
      {
        path.Stroke = activeColor;
      }
    }

    private void ComboBoxBorder_MouseLeave(object sender, MouseEventArgs e)
    {
      var border = (Border)sender;
      var path = FindChild<Path>(border);

      var defaultColor = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];
      if (path != null)
      {
        path.Stroke = defaultColor;
      }
    }

    private T FindChild<T>(DependencyObject parent, string childName = null) where T : FrameworkElement
    {
      if (parent == null)
      {
        return null;
      }

      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);

        if (child is T foundChild)
        {
          if (string.IsNullOrEmpty(childName))
          {
            return foundChild;
          }

          if (foundChild.Name == childName)
          {
            return foundChild;
          }
        }

        var result = FindChild<T>(child, childName);
        if (result != null)
        {
          return result;
        }
      }

      return null;
    }

    private void PART_ContentPresenter_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (searchArrowsComboBox.IsDropDownOpen)
      {
        LogInformation("Выпадающий список открыт, отменяем поиск.");
        return;
      }

      if (searchArrowsComboBox.SelectedItem is ArrowItem selectedArrow)
      {
        var arrowType = searchArrowsComboBox.SelectedItem as ArrowItem;
        if (arrowType != null)
        {
          LogInformation($"Запуск поиска для: {selectedArrow.Name}");
          SearchEventAdapter.RaiseSearchButtonPressed(selectedArrow.Name);
        }
      }
    }

    private void searchArrowsComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var clickedElement = e.OriginalSource as FrameworkElement;

      if (clickedElement is ComboBoxItem)
      {
        Debug.WriteLine("Клик по элементу списка. Прерываем обработку.");
        return;
      }

      if (clickedElement is Border border)
      {
        var path = FindChild<Path>(border);
        if (path != null)
        {
          e.Handled = true;
          PART_ContentPresenter_PreviewMouseDown(sender, e);
        }
      }
    }
  }
}
