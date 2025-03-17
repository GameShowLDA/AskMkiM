using Mode.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mode.ServicesTest.MKR
{
  /// <summary>
  /// Логика взаимодействия для MkrControl.xaml.
  /// Этот контрол предназначен для отображения и управления точками измерения устройства.
  /// </summary>
  public partial class MkrControl : UserControl
  {
    /// <summary>
    /// Флаг, указывающий, что устройство МКР было инициализировано.
    /// </summary>
    private bool isMkrInitialized = false;

    /// <summary>
    /// Флаг подключения устройства.
    /// </summary>
    private bool isConnected = false;

    /// <summary>
    /// Текущее имя выбранного устройства.
    /// </summary>
    private string currentDeviceName = string.Empty;

    /// <summary>
    /// Общее количество точек, используемых в контроле.
    /// </summary>
    public readonly short numPoints = 350;

    /// <summary>
    /// Коллекция точек для привязки к пользовательскому интерфейсу.
    /// </summary>
    private ObservableCollection<MkrPointModel> points;

    /// <summary>
    /// Список всех точек, если требуется дополнительная логика работы с коллекцией.
    /// </summary>
    private System.Collections.Generic.List<MkrPointModel> allPoints;

    /// <summary>
    /// Представление коллекции точек для фильтрации.
    /// </summary>
    private ICollectionView pointsView;

    /// <summary>
    /// ViewModel для ComboBox.
    /// </summary>
    private ViewModel _viewModel;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MkrControl"/>.
    /// Устанавливает DataContext и инициализирует коллекцию точек.
    /// </summary>
    public MkrControl()
    {
      InitializeComponent();

      _viewModel = new ViewModel();
      DataContext = _viewModel;

      InitializePoints();

      // По умолчанию кнопки "ЗАПУСТИТЬ" и "СБРОС" могут быть отключены, если требуется дополнительная логика
      // BtnConnect.IsEnabled = false;
      // BtnMkrReset.IsEnabled = false;
    }

    /// <summary>
    /// Инициализирует коллекцию точек от 1 до <see cref="numPoints"/> и устанавливает представление для фильтрации.
    /// </summary>
    private void InitializePoints()
    {
      points = new ObservableCollection<MkrPointModel>();
      // Если требуется, можно также использовать allPoints для дополнительной логики,
      // но для фильтрации достаточно коллекции points.
      for (short i = 1; i <= numPoints; i++)
      {
        points.Add(new MkrPointModel(i));
      }

      pointsView = CollectionViewSource.GetDefaultView(points);
      PointsListBox.ItemsSource = pointsView;
    }
  }
}