using Mode.Models;
using Mode.ServicesTest.Helpers;
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

namespace Mode.ServicesTest.UKSH
{
  /// <summary>
  /// Логика взаимодействия для UkshControl.xaml
  /// </summary>
  public partial class UkshControl : UserControl
  {
    private bool isUkshInitialized = false;
    private string currentDeviceName = string.Empty;
    private bool isShinaConnected = false;

    // Список реле (полный и отображаемый)
    private List<RelayModel> allRelays;
    public ObservableCollection<RelayModel> Relays { get; set; }

    // ViewModel для ComboBox (например, ComboBoxViewModel)
    private ViewModel ViewModel;

    public UkshControl()
    {
      InitializeComponent();

      // Инициализируем ViewModel
      ViewModel = new ViewModel();
      // добавим нужные пункты, если нет
      // comboBoxViewModel.ComboBoxItems = new ObservableCollection<string> { "<пусто>", "Устройство 1", "Устройство 2" };

      // Привязываем ComboBox к данным
      CmbUkshInit.ItemsSource = ViewModel.ComboBoxItems;
      CmbUkshInit.SelectedItem = ViewModel.SelectedComboBoxItem;

      // Инициализация реле
      allRelays = new List<RelayModel>();
      Relays = new ObservableCollection<RelayModel>();

      InitializeRelays(); // создаём 1..100

      IcRelays.ItemsSource = Relays;

      FilterRelays("");   // показать все
    }

    private void InitializeRelays()
    {
      RelayModel relay;
      for (short i = 1; i <= 100; i++)
      {
        relay = new RelayModel(i);
        allRelays.Add(relay);
        Relays.Add(relay);
      }
    }
  }
}
