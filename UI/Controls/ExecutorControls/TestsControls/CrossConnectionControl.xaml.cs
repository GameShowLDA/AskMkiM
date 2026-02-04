using Ask.Engine.Tests.RelaySwitchingModule;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls
{
  /// <summary>
  /// Логика взаимодействия для CrossConnectionControl.xaml
  /// </summary>
  public partial class CrossConnectionControl : UserControl
  {
    private CrossConnectionTests crossConnectionTests = new CrossConnectionTests();
    public CrossConnectionControl()
    {
      InitializeComponent();

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "CrossTestMkr");
      };

      _ = crossConnectionTests.InitializeSettingsAsync(ProtocolSelfCheckControl, ProtocolSelfCheckControl);
    }
  }
}