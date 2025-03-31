using System.Windows.Controls;

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
    }
  }
}
