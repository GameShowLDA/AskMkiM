using Mode.Models;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mode.ServicesTest.UKSH
{
  /// <summary>
  /// Пользовательский контрол для работы с устройством UKSH.
  /// Отвечает за инициализацию списка реле, фильтрацию и привязку данных.
  /// </summary>
  public partial class UkshControl : UserControl
  {
    /// <summary>
    /// Флаг, указывающий, что устройство UKSH было инициализировано.
    /// </summary>
    private bool isUkshInitialized = false;

    /// <summary>
    /// Текущее выбранное имя устройства.
    /// </summary>
    private string currentDeviceName = string.Empty;

    /// <summary>
    /// Флаг, показывающий, подключена ли шина.
    /// </summary>
    private bool isShinaConnected = false;

    /// <summary>
    /// Полный список реле.
    /// </summary>
    private List<RelayModel> allRelays;

    /// <summary>
    /// Представление коллекции для фильтрации списка реле.
    /// </summary>
    private ICollectionView relaysView;

    /// <summary>
    /// ViewModel для ComboBox, содержащий список устройств.
    /// </summary>
    private ViewModel ViewModel;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UkshControl"/>.
    /// Настраивает компоненты, привязывает ComboBox и инициализирует список реле.
    /// </summary>
    public UkshControl()
    {
      InitializeComponent();

      ViewModel = new ViewModel();
      CmbUkshInit.ItemsSource = ViewModel.ComboBoxItems;
      CmbUkshInit.SelectedItem = ViewModel.SelectedComboBoxItem;

      allRelays = new List<RelayModel>();

      InitializeRelays();

      IcRelays.ItemsSource = allRelays;

      FilterRelays("");
    }

    /// <summary>
    /// Создает 100 экземпляров <see cref="RelayModel"/>, добавляет их в список и настраивает представление коллекции для фильтрации.
    /// </summary>
    private void InitializeRelays()
    {
      for (short i = 1; i <= 100; i++)
      {
        var relay = new RelayModel(i);
        allRelays.Add(relay);
      }
      relaysView = CollectionViewSource.GetDefaultView(allRelays);
    }
  }
}