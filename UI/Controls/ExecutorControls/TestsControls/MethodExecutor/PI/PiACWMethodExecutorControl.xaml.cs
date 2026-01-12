using Ask.Engine.Tests.MethodExecutor.PI;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls.MethodExecutor.PI
{
  /// <summary>
  /// Логика взаимодействия для PiACWMethodExecutorControl.xaml
  /// </summary>
  public partial class PiACWMethodExecutorControl : UserControl
  {
    private PiAcwGroupMethodExecutor mode = new PiAcwGroupMethodExecutor();

    public PiACWMethodExecutorControl()
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
