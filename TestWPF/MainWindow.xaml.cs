using System.Windows;
using System.Windows.Media;
using Utilities.Models;

namespace TestWPF
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      TestControl.Background = new SolidColorBrush(Colors.Black);
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings()
    {
      var RESILT = MessageBox.Show("Тест 1", string.Empty, MessageBoxButton.OKCancel, MessageBoxImage.Hand);
      UI.Controls.Message.MessageBox.Show(UI.Controls.Message.MessageBox.Status.Warning, "Предупреждение тест 1", "Тестирую предупреждение", MessageBoxButton.OK);
      UI.Controls.Message.MessageBox.Show(UI.Controls.Message.MessageBox.Status.Error, "Ошибка тест 1", "Тестирую ошибка", MessageBoxButton.YesNo);
      UI.Controls.Message.MessageBox.Show(UI.Controls.Message.MessageBox.Status.Information, "Информация тест 1", "Тестирую информацию", MessageBoxButton.OKCancel);
      UI.Controls.Message.MessageBox.Show(UI.Controls.Message.MessageBox.Status.Question, "Информация тест 1", "Тестирую информацию", MessageBoxButton.OKCancel);
    }
  }
}
