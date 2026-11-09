using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Ask.UI.Components.ProtocolListBox;

/// <summary>
/// Предоставляет посимвольное выделение строки протокола без дублирующего узла автоматизации.
/// </summary>
public sealed class ProtocolSelectableTextBox : TextBox
{
  /// <inheritdoc />
  protected override AutomationPeer? OnCreateAutomationPeer() => null;
}
