using Ask.Engine.Tests.MethodExecutor.CI;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls.MethodExecutor.CI
{
  /// <summary>
  /// Логика взаимодействия для CiMethodExecutor.xaml
  /// </summary>
  public partial class CiMethodExecutor : UserControl
  {

    private CiGroupMethodExecutor mode = new CiGroupMethodExecutor();

    public CiMethodExecutor()
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
