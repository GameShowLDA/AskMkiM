using Ask.Engine.Tests.NodeMethod.CI;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls.NodeMethod.CI
{
  /// <summary>
  /// Логика взаимодействия для CiNodeMethodControl.xaml
  /// </summary>
  public partial class CiNodeMethodControl : UserControl
  {
    private CiNodeMethodExecutor mode = new CiNodeMethodExecutor();
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
