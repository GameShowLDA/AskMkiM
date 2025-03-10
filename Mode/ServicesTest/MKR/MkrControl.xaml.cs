using Mode.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mode.ServicesTest.MKR
{
  /// <summary>
  /// Логика взаимодействия для MkrControl.xaml
  /// </summary>
  public partial class MkrControl : UserControl
  {
    // Флаги
    private bool isMkrInitialized = false;
    private bool isConnected = false;
    private string currentDeviceName = string.Empty;

    // Коллекции точек
    private ObservableCollection<MkrPointModel> points;
    private System.Collections.Generic.List<MkrPointModel> allPoints;

    // ViewModel для ComboBox
    private ViewModel _viewModel;

    public MkrControl()
    {
      InitializeComponent();

      // Пример инициализации ViewModel
      _viewModel = new ViewModel();
      this.DataContext = _viewModel;

      // Можно сразу инициализировать список точек
      InitializePoints();

      // Кнопка "ЗАПУСТИТЬ" по умолчанию недоступна,
      // но если надо - можно что-то настроить
      // BtnConnect.IsEnabled = false;
      // BtnMkrReset.IsEnabled = false;
    }

    /// <summary>
    /// Заполняет список точек (1..350).
    /// </summary>
    private void InitializePoints()
    {
      points = new ObservableCollection<MkrPointModel>();
      allPoints = new System.Collections.Generic.List<MkrPointModel>();

      for (short i = 1; i <= 350; i++)
      {
        var point = new MkrPointModel(i);
        points.Add(point);
        allPoints.Add(point);
      }

      PointsListBox.ItemsSource = points;
    }
  }
}
