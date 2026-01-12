using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  /// <summary>
  /// Логика взаимодействия для IeMetrologyControl.xaml
  /// </summary>
  public partial class IeMetrologyControl : UserControl
  {

    private readonly ModeIE mode = new ModeIE();

    public IeMetrologyControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI, ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "UtilityModeIE");
      };
    }
  }
}
