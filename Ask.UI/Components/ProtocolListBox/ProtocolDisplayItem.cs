using Ask.Core.Services.Config.Base;
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
    private bool _hasChildItems;
    private bool _isLastGroupItem;
    private Thickness _outerMargin;

    private ProtocolDisplayItem(
      ShowMessageModel message,
      bool isCommandHeader,
      bool isInsideCommandGroup,
      Thickness outerMargin,
      ProtocolCommandGroup? group = null)
    {
      Message = message;
      IsCommandHeader = isCommandHeader;
      IsInsideCommandGroup = isInsideCommandGroup;
      _outerMargin = outerMargin;
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
    public bool IsInsideCommandGroup { get; }

    public FontWeight CommandFontWeight => IsCommandHeader && UserInterfaceConfig.GetCommandBodyBackgroundHighlighting()
      ? FontWeights.Bold
      : FontWeights.Normal;

    public GridLength GutterWidth => HasChildItems || IsInsideCommandGroup
      ? new GridLength(24)
      : new GridLength(0);

    public Thickness OuterMargin
    {
      get => _outerMargin;
      set
      {
        if (_outerMargin == value)
        {
          return;
        }

        _outerMargin = value;
        OnPropertyChanged();
      }
    }

    public bool HasChildItems
    {
      get => _hasChildItems;
      set
      {
        if (_hasChildItems == value)
        {
          return;
        }

        _hasChildItems = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(GutterWidth));
      }
    }

    public bool IsLastGroupItem
    {
      get => _isLastGroupItem;
      set
      {
        if (_isLastGroupItem == value)
        {
          return;
        }

        _isLastGroupItem = value;
        OnPropertyChanged();
      }
    }

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
        isInsideCommandGroup: false,
        outerMargin: new Thickness(0, 0, 0, 6),
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
        isInsideCommandGroup: isInsideCommandGroup,
        outerMargin: new Thickness(0, 0, 0, 6));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
