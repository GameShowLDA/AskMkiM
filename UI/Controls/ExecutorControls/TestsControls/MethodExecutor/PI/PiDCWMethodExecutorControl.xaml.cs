using Ask.Engine.Tests.MethodExecutor.PI;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls.MethodExecutor.PI
{
  /// <summary>
  /// Логика взаимодействия для PiDCWMethodExecutorControl.xaml
  /// </summary>
  public partial class PiDCWMethodExecutorControl : UserControl
  {

    private PiDcwGroupMethodExecutor mode = new PiDcwGroupMethodExecutor();
    public PiDCWMethodExecutorControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "TestPIGroupMethod");
      };
    }
  }
}
