using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;
using UI.Windows;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  /// <summary>
  /// Логика взаимодействия для KnDCWMetrologyControl.xaml
  /// </summary>
  public partial class KnDCWMetrologyControl : UserControl
  {
    private readonly ModeKnDcw mode = new ModeKnDcw();
    public KnDCWMetrologyControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI, ProtocolUI, new VoltageValue());

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "UtilityModeKN");
      };
    }
  }
}
