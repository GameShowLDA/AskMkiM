using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using Ask.Engine.Tests.Metrology;
using Ask.Support;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.MetrologyControls
{
  [MetrologyMode(MetrologyType.PR, "Режим ПР")]

  /// <summary>
  /// Логика взаимодействия для PrMetrologyControl.xaml
  /// </summary>
  public partial class PrMetrologyControl : UserControl
  {

    private readonly ModePr mode = new ModePr();

    public PrMetrologyControl()
    {
      InitializeComponent();

      mode.InitializeSettings(ProtocolUI, ProtocolUI);

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "UtilityModePR");
      };
    }
  }
}
