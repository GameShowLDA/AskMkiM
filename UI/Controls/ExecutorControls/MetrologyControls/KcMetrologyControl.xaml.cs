using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  /// <summary>
  /// Логика взаимодействия для KcMetrologyControl.xaml
  /// </summary>
  public partial class KcMetrologyControl : UserControl
  {
    private readonly ModeKC mode = new ModeKC();
    public KcMetrologyControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI, ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "UtilityModeKC");
      };
    }
  }
}
