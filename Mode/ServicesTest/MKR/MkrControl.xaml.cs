using DataBaseConfiguration.Services.Device;
using Mode.Models;
using NewCore.Base.Interface.Main;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using static Utilities.Models.ShowMessageModel;

namespace Mode.ServicesTest.MKR
{
  /// <summary>
  /// Логика взаимодействия для MkrControl.xaml.
  /// Этот контроллер предназначен для отображения и управления точками измерения устройства.
  /// </summary>
  public partial class MkrControl : UserControl
  {
    private MkrContent _content;

    //ProtocolUI ProtocolSelfCheckControl;

    /// <summary>
    /// Статусное сообщение для успешного выполнения теста.
    /// </summary>
    private readonly (string Title, Color TitleColor) goodText = SuccessMessage;

    /// <summary>
    /// Статусное сообщение для ошибки в процессе выполнения теста.
    /// </summary>
    private readonly (string Title, Color TitleColor) errorText = ErrorMessage;

    /// <summary>
    /// Коллекция точек для привязки к пользовательскому интерфейсу.
    /// </summary>
    private ObservableCollection<MkrPointModel> points;

    /// <summary>
    /// Представление коллекции точек для фильтрации.
    /// </summary>
    private ICollectionView pointsView;

    /// <summary>
    /// ViewModel для ComboBox.
    /// </summary>
    private ViewModel _viewModel;

    /// <summary>
    /// Сервис для работы с модулями коммутации.
    /// </summary>
    private readonly RelaySwitchModuleServices _relayService;

    /// <summary>
    /// Текущее выбранное устройство модуля коммутации.
    /// </summary>
    private IRelaySwitchModule currentDevice;

    /// <summary>
    /// Список всех доступных устройств модулей коммутации.
    /// </summary>
    private List<IRelaySwitchModule> _devices;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MkrControl"/>.
    /// Устанавливает DataContext и инициализирует коллекцию точек.
    /// </summary>
    public MkrControl()
    {
      InitializeComponent();

      HideProtocolSelfCheckControlButtons();

      _viewModel = new ViewModel();
      _relayService = new RelaySwitchModuleServices();
      DataContext = _viewModel;

      points = new ObservableCollection<MkrPointModel>();
      pointsView = CollectionViewSource.GetDefaultView(points);

      LoadDevicesIntoCombo();

      if (ProtocolSelfCheckControl.ContentView is MkrContent content)
      {
        _content = content;
        _content.ParentControl = this;

        _content.DataContext = _viewModel;

        _content.PointsListBox.ItemsSource = pointsView;

        currentBus = _content.RbOff;
      }
    }

    private void HideProtocolSelfCheckControlButtons()
    {
      ProtocolSelfCheckControl.StartMeasureResistanceButtonVisibility = Visibility.Collapsed;
      ProtocolSelfCheckControl.ReturnMeasureResistanceButtonVisibility = Visibility.Collapsed;
      ProtocolSelfCheckControl.LoopMeasureResistanceButtonVisibility = Visibility.Collapsed;
      ProtocolSelfCheckControl.PauseButtonVisibility = Visibility.Collapsed;
      ProtocolSelfCheckControl.StepOverButtonVisibility = Visibility.Collapsed;
      ProtocolSelfCheckControl.StepIntoButtonVisibility = Visibility.Collapsed;
      ProtocolSelfCheckControl.NextButtonVisibility = Visibility.Collapsed;
      ProtocolSelfCheckControl.ExitButtonVisibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Подгружает список модулей из БД и кладёт их в ComboBoxItems View-Model’и.
    /// </summary>
    private void LoadDevicesIntoCombo()
    {
      _devices = _relayService.GetAllDevices();

      _viewModel.ComboBoxItems.Clear();
      _viewModel.ComboBoxItems.Add("<пусто>");

      foreach (var d in _devices)
      {
        _viewModel.ComboBoxItems.Add($"{d.NumberChassis}.{d.Number}");
      }

      _viewModel.SelectedComboBoxItem = _viewModel.ComboBoxItems[0];
    }

    /// <summary>
    /// Инициализирует коллекцию точек и устанавливает представление для фильтрации.
    /// </summary>
    private void InitializePoints()
    {
      points = new ObservableCollection<MkrPointModel>();
      for (short i = 1; i <= currentDevice.PointCount; i++)
      {
        points.Add(new MkrPointModel(i));
      }

      pointsView = CollectionViewSource.GetDefaultView(points);
      _content.PointsListBox.ItemsSource = pointsView;
    }
  }
}