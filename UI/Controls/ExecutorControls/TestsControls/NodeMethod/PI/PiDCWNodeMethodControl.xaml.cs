using Ask.Engine.Tests.NodeMethod.PI;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls.NodeMethod.PI
{
  /// <summary>
  /// Логика взаимодействия для PiDCWNodeMethodControl.xaml
  /// </summary>
  public partial class PiDCWNodeMethodControl : UserControl
  {

    private PiDCWNodeMethodExecutor mode = new PiDCWNodeMethodExecutor();

    public PiDCWNodeMethodControl()
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
