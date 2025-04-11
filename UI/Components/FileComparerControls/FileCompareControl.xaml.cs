using System.Windows;
using System.Windows.Controls;

namespace UI.Components.FileComparerControls
{
  /// <summary>
  /// Логика взаимодействия для FileCompareControl.xaml
  /// </summary>
  public partial class FileCompareControl : UserControl
  {
    public FileCompareControl()
    {
      InitializeComponent();
    }

    private void UpDown_Click(object sender, RoutedEventArgs e)
    {
      // Показать горизонтальные
      TopBox.Visibility = Visibility.Visible;
      BottomBox.Visibility = Visibility.Visible;
      ContentGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
      ContentGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
      ContentGrid.RowDefinitions[1].Height = new GridLength(5);

      // Скрыть вертикальные
      LeftBox.Visibility = Visibility.Collapsed;
      RightBox.Visibility = Visibility.Collapsed;
      ContentGrid.ColumnDefinitions[0].Width = new GridLength(0);
      ContentGrid.ColumnDefinitions[2].Width = new GridLength(0);
      ContentGrid.ColumnDefinitions[1].Width = new GridLength(0);
    }

    private void LeftRight_Click(object sender, RoutedEventArgs e)
    {
      // Показать вертикальные
      LeftBox.Visibility = Visibility.Visible;
      RightBox.Visibility = Visibility.Visible;
      ContentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
      ContentGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
      ContentGrid.ColumnDefinitions[1].Width = new GridLength(5);

      // Скрыть горизонтальные
      TopBox.Visibility = Visibility.Collapsed;
      BottomBox.Visibility = Visibility.Collapsed;
      ContentGrid.RowDefinitions[0].Height = new GridLength(0);
      ContentGrid.RowDefinitions[2].Height = new GridLength(0);
      ContentGrid.RowDefinitions[1].Height = new GridLength(0);
    }

  }
}
