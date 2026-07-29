using System.Windows.Media;

namespace UI.Controls.AdminPanel.Commands
{
  /// <summary>
  /// Содержит отображаемые сведения об одном обмене с устройством.
  /// </summary>
  internal sealed class CommandExchangeViewModel
  {
    public string Endpoint { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Response { get; set; } = "Ожидание ответа…";
    public string Duration { get; set; } = string.Empty;
    public Brush StatusBrush { get; set; } = Brushes.Khaki;
  }
}
