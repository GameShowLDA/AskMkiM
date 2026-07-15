using Ask.Core.Shared.Metadata.Enums.FileEnums;
using System.Windows.Controls;

namespace Ask.UI.Controls.ProtocolNew
{
  /// <summary>
  /// Отображает связанный протокол выполнения и итоговый протокол.
  /// </summary>
  public partial class SavedProtocolPairUI : UserControl
  {
    public SavedProtocolPairUI(string executionProtocolText, string resultProtocolText)
    {
      InitializeComponent();

      ExecutionProtocolEditor.SetFileType(FileType.Protocol);
      ExecutionProtocolEditor.Text = executionProtocolText ?? string.Empty;

      ResultProtocolEditor.SetFileType(FileType.InspectionProtocol);
      ResultProtocolEditor.Text = resultProtocolText ?? string.Empty;
    }
  }
}
