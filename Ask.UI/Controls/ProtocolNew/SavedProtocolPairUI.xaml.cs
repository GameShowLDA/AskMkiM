using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.DTO.Protocol;
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
      ResultProtocolEditor.WordWrap = true;
      ResultProtocolEditor.Text = resultProtocolText ?? string.Empty;
    }

    public SavedProtocolPairUI(
      IReadOnlyList<ShowMessageModel> executionMessages,
      string resultProtocolText)
      : this(string.Empty, resultProtocolText)
    {
      ExecutionProtocolEditor.Visibility = System.Windows.Visibility.Collapsed;
      StructuredExecutionProtocol.Content = new SavedExecutionProtocolUI(executionMessages);
      StructuredExecutionProtocol.Visibility = System.Windows.Visibility.Visible;
    }
  }
}
