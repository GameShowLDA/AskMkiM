using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  /// <summary>
  /// Логика взаимодействия для EhtMetrologyControl.xaml
  /// </summary>
  public partial class EhtMetrologyControl : UserControl
  {

    private readonly ModeEht mode = new ModeEht();

    public EhtMetrologyControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI, ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "UtilityModeEht");
      };
    }
  }
}
