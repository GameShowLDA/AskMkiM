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
    /// <summary>
    /// Указывает, является ли текущий элемент отображения группой,
    /// содержащей дочерние элементы протокола.
    /// </summary>
    /// <remarks>
    /// Значение <see langword="true"/> означает, что элемент представляет
    /// сворачиваемый групповой узел.
    /// </remarks>
    private readonly bool _isGroup;

    /// <summary>
    /// Хранит текущее состояние развёрнутости группового элемента.
    /// </summary>
    /// <remarks>
    /// Значение <see langword="true"/> означает, что группа раскрыта.
    /// </remarks>
    private bool _isExpanded = true;

    /// <summary>
    /// Задний фон заголовка с успешно выполненными тестами.
    /// </summary>
    private Color _successBackground = Color.FromArgb(128, 94, 127, 107);

    /// <summary>
    /// Задний фон заголовка с ошибками в тестах.
    /// </summary>
    private Color _errorBackground = Color.FromArgb(128, 168, 93, 93);

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

    /// <summary>
    /// Обновляет цвет фона текущего элемента отображения протокола
    /// в соответствии с его текущим состоянием и связанными данными.
    /// </summary>
    public void UpdateBackgroundColor()
    {
      if (!IsGroup || Message == null) return;

      bool hasError = Children.Any(child => child.Message?.Status == ShowMessageModel.MessageType.Error);

      Color backgroundColor = hasError ? _errorBackground : _successBackground;

      Message.HeaderBackgroundColor = backgroundColor;
    }

    /// <summary>
    /// Создаёт группу.
    /// </summary>
    public static ProtocolDisplayItem CreateGroup(ShowMessageModel headerModel, bool isExpanded = true)
    {
      return new ProtocolDisplayItem(true)
      {
        Message = headerModel,
        IsExpanded = isExpanded
      };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}