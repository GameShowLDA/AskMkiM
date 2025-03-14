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

namespace Mode.ServicesTest.MESH
{
  /// <summary>
  /// Логика взаимодействия для MeshControl.xaml
  /// </summary>
  public partial class MeshControl : UserControl
  {
    private bool isMeshInitialized = false;
    private string currentDeviceName = string.Empty;

    // ViewModel для ComboBox
    private ViewModel comboBoxViewModel;

    public MeshControl()
    {
      InitializeComponent();

      // Инициализируем ViewModel
      comboBoxViewModel = new ViewModel();
      // Привязываем к DataContext, чтобы ComboBoxItems / SelectedComboBoxItem стали доступны
      this.DataContext = comboBoxViewModel;

      // Начальная настройка UI (не инициализировано)
      InitializeMeshUI();

      // Сразу вызываем UpdateMeshUI(false, skipLog:true), 
      // чтобы зафиксировать стартовое состояние (кнопка питания отключена)
      //_ = UpdateMeshUI(false, true);
    }
  }
}
