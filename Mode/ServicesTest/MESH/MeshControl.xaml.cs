using Mode.Models;
using System.Windows.Controls;

namespace Mode.ServicesTest.MESH
{
  /// <summary>
  /// Логика взаимодействия для MeshControl.xaml.
  /// Этот контрол предназначен для управления устройством MESH.
  /// </summary>
  public partial class MeshControl : UserControl
  {
    /// <summary>
    /// Флаг, указывающий, что устройство MESH инициализировано.
    /// </summary>
    private bool isMeshInitialized = false;

    /// <summary>
    /// Текущее выбранное имя устройства.
    /// </summary>
    private string currentDeviceName = string.Empty;

    /// <summary>
    /// ViewModel для ComboBox, содержащего список устройств.
    /// </summary>
    private ViewModel comboBoxViewModel;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeshControl"/>.
    /// Настраивает DataContext и начальное состояние пользовательского интерфейса.
    /// </summary>
    public MeshControl()
    {
      InitializeComponent();

      // Инициализируем ViewModel для ComboBox.
      comboBoxViewModel = new ViewModel();
      DataContext = comboBoxViewModel;

      // Настраиваем начальное состояние UI (устройство не инициализировано).
      InitializeMeshUI();

      // Можно сразу вызвать UpdateMeshUI(false, skipLog:true) для установки стартового состояния.
      // _ = UpdateMeshUI(false, true);
    }
  }
}