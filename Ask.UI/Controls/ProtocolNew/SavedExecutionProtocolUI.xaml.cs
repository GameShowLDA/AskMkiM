using Ask.Core.Shared.DTO.Protocol;
using System.Windows.Controls;
using Ask.Core.Services.Protocols;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.EventCore.Events;

namespace Ask.UI.Controls.ProtocolNew;

/// <summary>
/// Отображает сохранённый структурированный протокол выполнения.
/// </summary>
public partial class SavedExecutionProtocolUI : UserControl
{
  private readonly string? _sourceText;
  private string? _filePath;
  public string? FilePath
  {
    get => _filePath;
    set
    {
      _filePath = value;
      ActionPanel.Visibility = string.IsNullOrWhiteSpace(value)
        ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }
  }

  private string GetOutputText() => string.Join(Environment.NewLine,
    Protocol.GetMessagesSnapshot().Select(ExecutionProtocolLineFormatter.Format));

  private async void PrintButton_Click(object sender, System.Windows.RoutedEventArgs e)
  {
    var text = GetOutputText();
    if (!string.IsNullOrWhiteSpace(text))
      await Ask.UI.Services.Notifications.PrintOperationNotificationService.PrintTextAsync(text, "Протокол выполнения");
  }

  private void ExportPdfButton_Click(object sender, System.Windows.RoutedEventArgs e)
  {
    var text = GetOutputText();
    if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(text))
      EventAggregator.Publish(new FileInteractionEvents.ExportProtocolPdf(FilePath, text));
  }

  public SavedExecutionProtocolUI(string sourceText)
  {
    InitializeComponent();
    _sourceText = sourceText;
    Loaded += (_, _) =>
    {
      EventAggregator.Unsubscribe<SystemStateEvents.DebugRightsChanged>(OnDebugRightsChanged);
      EventAggregator.Subscribe<SystemStateEvents.DebugRightsChanged>(OnDebugRightsChanged);
    };
    Unloaded += (_, _) => EventAggregator.Unsubscribe<SystemStateEvents.DebugRightsChanged>(OnDebugRightsChanged);
    Reload();
  }

  private void OnDebugRightsChanged(SystemStateEvents.DebugRightsChanged e) => Dispatcher.Invoke(Reload);

  private void Reload()
  {
    if (_sourceText == null) return;
    if (!ExecutionProtocolDiagnosticFormatter.TryRestoreMessages(_sourceText, DebugAccessConfig.IsDebugEnabled, out var messages))
      messages = ExecutionProtocolDiagnosticFormatter.RestoreLegacyMessages(_sourceText, DebugAccessConfig.IsDebugEnabled);
    Protocol.LoadMessages(messages);
  }

  public SavedExecutionProtocolUI(IEnumerable<ShowMessageModel> messages)
  {
    InitializeComponent();
    Protocol.LoadMessages(messages);
  }
}
