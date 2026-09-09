using Ask.Engine.Tests.MethodExecutor.Si;
using Ask.Support;
using System.Windows.Controls;

namespace Ask.UI.Controls.ExecutorControls.TestsControls.MethodExecutor.Si
{
  /// <summary>
  /// Логика взаимодействия для CiMethodExecutor.xaml
  /// </summary>
  public partial class SiMethodExecutor : UserControl
  {

    private SiGroupMethodExecutor mode = new SiGroupMethodExecutor();

    public SiMethodExecutor()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "TestSIGroupMethod");
      };
    }
  }
}
