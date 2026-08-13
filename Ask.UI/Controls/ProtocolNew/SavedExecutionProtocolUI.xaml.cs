using Ask.Core.Shared.DTO.Protocol;
using System.Windows.Controls;

namespace Ask.UI.Controls.ProtocolNew;

/// <summary>
/// Отображает сохранённый структурированный протокол выполнения.
/// </summary>
public partial class SavedExecutionProtocolUI : UserControl
{
  public SavedExecutionProtocolUI(IEnumerable<ShowMessageModel> messages)
  {
    InitializeComponent();
    Protocol.LoadMessages(messages);
  }
}
