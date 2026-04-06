using Ask.Core.Shared.DTO.Protocol;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Ask.UI.Components.ProtocolListBox
{
  /// <summary>
  /// Плоский элемент отображения протокола.
  /// Один объект соответствует одной видимой строке ListBox.
  /// </summary>
  public sealed class ProtocolDisplayItem : INotifyPropertyChanged
  {
    private bool _isExpanded = true;

    private ProtocolDisplayItem(
      ShowMessageModel message,
      bool isCommandHeader,
      Thickness containerMargin,
      ProtocolCommandGroup? group = null)
    {
      Message = message;
      IsCommandHeader = isCommandHeader;
      ContainerMargin = containerMargin;
      Group = group;
    }

    /// <summary>
    /// Сообщение, отображаемое в строке.
    /// </summary>
    public ShowMessageModel Message { get; }

    /// <summary>
    /// Признак заголовка главной команды.
    /// Только такие строки можно сворачивать и разворачивать.
    /// </summary>
    public bool IsCommandHeader { get; }

    /// <summary>
    /// Отступ контейнера строки относительно левого края списка.
    /// </summary>
    public Thickness ContainerMargin { get; }

    /// <summary>
    /// Ссылка на состояние группы команды для строки-заголовка.
    /// Для обычных строк равно <see langword="null"/>.
    /// </summary>
    internal ProtocolCommandGroup? Group { get; }

    /// <summary>
    /// Признак раскрытия группы команды.
    /// Используется только для отображения положения шеврона.
    /// </summary>
    public bool IsExpanded
    {
      get => _isExpanded;
      set
      {
        if (_isExpanded == value)
        {
          return;
        }

        _isExpanded = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Создаёт строку-заголовок главной команды.
    /// </summary>
    internal static ProtocolDisplayItem CreateCommandHeader(ShowMessageModel model, ProtocolCommandGroup group)
    {
      return new ProtocolDisplayItem(
        model,
        isCommandHeader: true,
        containerMargin: new Thickness(0),
        group: group);
    }

    /// <summary>
    /// Создаёт обычную строку тела команды или корневую строку без группы.
    /// </summary>
    public static ProtocolDisplayItem CreateLine(ShowMessageModel model, bool isInsideCommandGroup)
    {
      return new ProtocolDisplayItem(
        model,
        isCommandHeader: false,
        containerMargin: isInsideCommandGroup ? new Thickness(22, 0, 0, 0) : new Thickness(0));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
