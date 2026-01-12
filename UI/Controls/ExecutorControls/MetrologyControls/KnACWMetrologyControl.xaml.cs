using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;
using UI.Windows;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  /// <summary>
  /// Логика взаимодействия для KnACWMetrologyControl.xaml
  /// </summary>
  public partial class KnACWMetrologyControl : UserControl
  {
    private readonly ModeKnAcw mode = new ModeKnAcw();

    public KnACWMetrologyControl()
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
