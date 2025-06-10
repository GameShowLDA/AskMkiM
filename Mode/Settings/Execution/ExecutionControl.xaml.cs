using System.Windows.Controls;
using AppConfiguration.Base;

namespace Mode.Settings.Execution
{
  /// <summary>
  /// Логика взаимодействия для ExecutionControl.xaml
  /// </summary>
  public partial class ExecutionControl : UserControl
  {
    static private bool start = false;

    public ExecutionControl()
    {
      start = false;
      InitializeComponent();
      SetConfiguration();
      start = true;

      Loaded += ExecutionControl_Loaded;
    }
  }
}
