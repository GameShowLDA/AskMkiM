using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  [MetrologyMode(MetrologyType.SI, "Режим СИ")]
  /// <summary>
  /// Логика взаимодействия для CiMetrologyControl.xaml
  /// </summary>
  public partial class CiMetrologyControl : UserControl
  {
    private readonly ModeCI mode = new ModeCI();

    public CiMetrologyControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI, ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "UtilityModeSI");
      };
    }
  }
}
