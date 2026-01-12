using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;
using UI.Windows;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  /// <summary>
  /// Логика взаимодействия для PiACWMetrologyControl.xaml
  /// </summary>
  public partial class PiACWMetrologyControl : UserControl
  {
    private readonly ModePiAcw mode = new ModePiAcw();

    public PiACWMetrologyControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI, ProtocolUI, new VoltageValue());

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "UtilityModePI");
      };
    }
  }
}
