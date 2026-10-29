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
    public event Action<string>? ExportPdfRequested;

    public void SetExecutionProtocolFilePath(string path)
    {
      if (StructuredExecutionProtocol.Content is SavedExecutionProtocolUI viewer)
        viewer.FilePath = path;
    }

    private async void PrintButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(ResultProtocolEditor.Text))
        await Ask.UI.Services.Notifications.PrintOperationNotificationService.PrintTextAsync(
          ResultProtocolEditor.Text, "Итоговый протокол");
    }

    private void ExportPdfButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(ResultProtocolEditor.Text))
        ExportPdfRequested?.Invoke(ResultProtocolEditor.Text);
    }

    public SavedProtocolPairUI(string executionProtocolText, string resultProtocolText)
    {
      InitializeComponent();

      ExecutionProtocolEditor.SetFileType(FileType.Protocol);
      ExecutionProtocolEditor.Visibility = System.Windows.Visibility.Collapsed;
      StructuredExecutionProtocol.Content = new SavedExecutionProtocolUI(executionProtocolText ?? string.Empty);
      StructuredExecutionProtocol.Visibility = System.Windows.Visibility.Visible;

      ResultProtocolEditor.SetFileType(FileType.InspectionProtocol);
      ResultProtocolEditor.WordWrap = true;
      ResultProtocolEditor.Text = resultProtocolText ?? string.Empty;
    }

    public SavedProtocolPairUI(
      IReadOnlyList<ShowMessageModel> executionMessages,
      string resultProtocolText)
    {
      InitializeComponent();
      ExecutionProtocolEditor.Visibility = System.Windows.Visibility.Collapsed;
      StructuredExecutionProtocol.Content = new SavedExecutionProtocolUI(executionMessages);
      StructuredExecutionProtocol.Visibility = System.Windows.Visibility.Visible;

      ResultProtocolEditor.SetFileType(FileType.InspectionProtocol);
      ResultProtocolEditor.WordWrap = true;
      ResultProtocolEditor.Text = resultProtocolText ?? string.Empty;
    }
  }
}
