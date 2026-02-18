using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;
using UI.Windows;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  [MetrologyMode(MetrologyType.PI_DCW, "Режим ПИ(DCW)")]

  /// <summary>
  /// Логика взаимодействия для PiDCWMetrologyControl.xaml
  /// </summary>
  public partial class PiDCWMetrologyControl : UserControl
  {
    private readonly ModePiDcw mode = new ModePiDcw();

    public PiDCWMetrologyControl()
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
