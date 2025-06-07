using Mode.Models;
using System.Windows.Controls;
using UI.Controls.ProtocolNew;

namespace Mode.ServicesTest.MINT
{
  /// <summary>
  /// Логика взаимодействия для MintControl.xaml.
  /// Управляет интерфейсом для устройства MINT, включая инициализацию и настройку компонентов.
  /// </summary>
  public partial class MintControl : UserControl
  {
    ProtocolUI ProtocolSelfCheckControl;

    /// <summary>
    /// Флаг, указывающий, что устройство MINT было инициализировано.
    /// </summary>
    private bool isDeviceInitialized = false;

    /// <summary>
    /// Текущее выбранное имя устройства.
    /// </summary>
    private string currentDeviceName = string.Empty;

    /// <summary>
    /// Флаг состояния кнопки заземления шины.
    /// </summary>
    private bool btnMintGroundStatus;

    /// <summary>
    /// Флаг подключения модуля ПИН.
    /// </summary>
    private bool isMintPinConnected = false;

    /// <summary>
    /// Флаг подключения модуля ПИТ.
    /// </summary>
    private bool isMintPitConnected = false;

    /// <summary>
    /// ViewModel для ComboBox выбора устройства.
    /// </summary>
    private ViewModel comboBoxViewModel;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MintControl"/>.
    /// Выполняет базовую инициализацию интерфейса и настраивает привязку для ComboBox.
    /// </summary>
    public MintControl()
    {
      InitializeComponent();

      // Инициализируем базовый UI
      InitializeMintUI();

      // Настраиваем ViewModel для ComboBox
      comboBoxViewModel = new ViewModel();
      CmbMintDevice.ItemsSource = comboBoxViewModel.ComboBoxItems;
      CmbMintDevice.SelectedItem = comboBoxViewModel.SelectedComboBoxItem;

      // Вызываем UpdateMintUI(false, skipLog: true) для установки начального состояния, если необходимо.
      // _ = UpdateMintUI(false, skipLog: true);
    }
  }
}