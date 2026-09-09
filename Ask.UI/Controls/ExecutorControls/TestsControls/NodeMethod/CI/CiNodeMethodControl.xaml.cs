using Ask.Engine.Tests.NodeMethod.Si;
using Ask.Support;
using System.Windows.Controls;

namespace Ask.UI.Controls.ExecutorControls.TestsControls.NodeMethod.CI
{
  /// <summary>
  /// Логика взаимодействия для CiNodeMethodControl.xaml
  /// </summary>
  public partial class CiNodeMethodControl : UserControl
  {
    private SiNodeMethodExecutor mode = new SiNodeMethodExecutor();
    public CiNodeMethodControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "TestSINodeMethod");
      };
    }
  }
}
