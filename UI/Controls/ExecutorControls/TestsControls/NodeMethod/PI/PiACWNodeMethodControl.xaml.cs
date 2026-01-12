using Ask.Engine.Tests.NodeMethod.PI;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls.NodeMethod.PI
{
  /// <summary>
  /// Логика взаимодействия для PiACWNodeMethodControl.xaml
  /// </summary>
  public partial class PiACWNodeMethodControl : UserControl
  {

    private PiACWNodeMethodExecutor mode = new PiACWNodeMethodExecutor();

    public PiACWNodeMethodControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI, ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "TestPINodeMethod");
      };
    }
  }
}
