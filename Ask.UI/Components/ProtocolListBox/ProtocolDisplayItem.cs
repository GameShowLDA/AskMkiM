using Ask.Core.Shared.DTO.Protocol;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Ask.UI.Components.ProtocolListBox
{
  /// <summary>
  /// Узел отображения протокола.
  /// Может быть либо обычной строкой, либо группой со сворачиваемым содержимым.
  /// </summary>
  public sealed class ProtocolDisplayItem : INotifyPropertyChanged
  {
    private readonly bool _isGroup;
    private bool _isExpanded = false;

    private ProtocolDisplayItem(bool isGroup)
    {
      _isGroup = isGroup;
    }

    /// <summary>
    /// Сообщение протокола.
    /// Для обычной строки — сама строка.
    /// Для группы — заголовок группы.
    /// </summary>
    public ShowMessageModel? Message { get; set; }

    /// <summary>
    /// Дочерние элементы группы.
    /// </summary>
    public ObservableCollection<ProtocolDisplayItem> Children { get; } = new();

    /// <summary>
    /// Признак того, что элемент является группой.
    /// </summary>
    public bool IsGroup => _isGroup;

    /// <summary>
    /// Признак развёрнутости группы.
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
    /// Создаёт обычную строку.
    /// </summary>
    public static ProtocolDisplayItem CreateLine(ShowMessageModel model)
    {
      return new ProtocolDisplayItem(false)
      {
        Message = model
      };
    }

    public void UpdateBackgroundColor()
    {
      if (!IsGroup || Message == null) return;

      bool hasError = Children.Any(child => child.Message?.Status == ShowMessageModel.MessageType.Error);

      Color backgroundColor = hasError ? Colors.Red : Colors.Green;

      Message.HeaderBackgroundColor = backgroundColor;
    }

    /// <summary>
    /// Создаёт группу.
    /// </summary>
    public static ProtocolDisplayItem CreateGroup(ShowMessageModel headerModel)
    {
      return new ProtocolDisplayItem(true)
      {
        Message = headerModel,
        IsExpanded = false
      };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}