using Mode.Models;
using System;
using System.Collections.Generic;
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

namespace Mode.ServicesTest.MINT
{
  /// <summary>
  /// Логика взаимодействия для MintControl.xaml
  /// </summary>
  public partial class MintControl : UserControl
  {
    private bool isDeviceInitialized = false;
    private string currentDeviceName = string.Empty;

    private bool btnMintGroundStatus;
    private bool isMintPinConnected = false;
    private bool isMintPitConnected = false;

    // ViewModel для ComboBox
    private ViewModel comboBoxViewModel;

    public MintControl()
    {
      InitializeComponent();

      // Инициализация базового UI
      InitializeMintUI();

      // Настраиваем ViewModel для ComboBox
      comboBoxViewModel = new ViewModel();
      CmbMintDevice.ItemsSource = comboBoxViewModel.ComboBoxItems;
      CmbMintDevice.SelectedItem = comboBoxViewModel.SelectedComboBoxItem;

      // Вызываем UpdateMintUI(false, skipLog: true), как в Uksh
      // чтобы зафиксировать, что пока всё выключено
      // (то есть "устройство не инициализировано").
      _ = UpdateMintUI(false, skipLog: true);
    }
  }
}
