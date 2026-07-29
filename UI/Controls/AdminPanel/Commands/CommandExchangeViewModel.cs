using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UI.Controls.AdminPanel.Commands
{
  /// <summary>
  /// Содержит отображаемые сведения об одном обмене с устройством.
  /// </summary>
  internal sealed class CommandExchangeViewModel : INotifyPropertyChanged
  {
    private string response = "Ожидание ответа…";
    private string duration = string.Empty;
    private Brush statusBrush = Brushes.Khaki;

    public string Endpoint { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;

    public string Response
    {
      get => response;
      set => SetField(ref response, value);
    }

    public string Duration
    {
      get => duration;
      set => SetField(ref duration, value);
    }

    public Brush StatusBrush
    {
      get => statusBrush;
      set => SetField(ref statusBrush, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
      if (EqualityComparer<T>.Default.Equals(field, value))
      {
        return;
      }

      field = value;
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
