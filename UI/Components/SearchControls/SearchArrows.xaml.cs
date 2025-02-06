using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для SearchArrows.xaml
  /// </summary>
  public partial class SearchArrows : UserControl
  {
    public SearchArrows()
    {
      InitializeComponent();
    }

    private void PART_ContentPresenter_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (searchArrowsComboBox != null)
      {
        searchArrowsComboBox.IsDropDownOpen = !searchArrowsComboBox.IsDropDownOpen;
      }
    }

    private void PART_ContentPresenter_MouseEnter(object sender, MouseEventArgs e)
    {
      var presenter = (ContentPresenter)sender;

      // Находим стрелку (Path) внутри ContentPresenter
      var path = FindChild<Path>(presenter);

      var activeColor = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      if (path != null)
        path.Stroke = activeColor;

      // Находим ToggleButton по имени
      var toggleButton = FindChild<ToggleButton>(presenter.Parent, "ComboBoxToggleButton");
      if (toggleButton != null)
        toggleButton.Foreground = activeColor;
    }

    private void PART_ContentPresenter_MouseLeave(object sender, MouseEventArgs e)
    {
      var presenter = (ContentPresenter)sender;

      // Находим стрелку (Path) внутри ContentPresenter
      var path = FindChild<Path>(presenter);

      var defaultColor = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];
      if (path != null)
        path.Stroke = defaultColor;

      // Находим ToggleButton по имени
      var toggleButton = FindChild<ToggleButton>(presenter.Parent, "ComboBoxToggleButton");
      if (toggleButton != null)
        toggleButton.Foreground = defaultColor;
    }


    // Метод для поиска дочернего элемента по имени
    private T FindChild<T>(DependencyObject parent, string childName = null) where T : FrameworkElement
    {
      if (parent == null) return null;

      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);

        // Проверяем, совпадает ли тип
        if (child is T foundChild)
        {
          // Если имя не указано, возвращаем первый найденный элемент нужного типа
          if (string.IsNullOrEmpty(childName))
            return foundChild;

          // Если имя указано, проверяем, совпадает ли оно
          if (foundChild.Name == childName)
            return foundChild;
        }

        // Рекурсивный поиск в дочерних элементах
        var result = FindChild<T>(child, childName);
        if (result != null)
          return result;
      }

      return null;
    }

  }

}
