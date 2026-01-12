using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls.MethodExecutor.PI
{
  /// <summary>
  /// Логика взаимодействия для PiDCWMethodExecutorControl.xaml
  /// </summary>
  public partial class PiDCWMethodExecutorControl : UserControl
  {
    public PiDCWMethodExecutorControl()
    {
      InitializeComponent();

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "TestPIGroupMethod");
      };
    }
  }
}
