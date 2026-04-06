using Ask.Core.Shared.DTO.Protocol;
using System.Collections.Generic;
using System.Windows.Media;

namespace Ask.UI.Components.ProtocolListBox
{
  /// <summary>
  /// Внутреннее состояние главной команды протокола.
  /// Хранит заголовок и накопленные строки команды без вложенного визуального дерева.
  /// </summary>
  internal sealed class ProtocolCommandGroup
  {
    private static readonly Color SuccessBackground = Color.FromArgb(128, 94, 127, 107);
    private static readonly Color ErrorBackground = Color.FromArgb(128, 168, 93, 93);

    private bool _hasErrors;

    public ProtocolCommandGroup(ShowMessageModel headerModel)
    {
      HeaderItem = ProtocolDisplayItem.CreateCommandHeader(headerModel, this);
    }

    /// <summary>
    /// Видимая строка-заголовок команды.
    /// </summary>
    public ProtocolDisplayItem HeaderItem { get; }

    /// <summary>
    /// Все строки тела команды, накопленные с момента старта команды.
    /// </summary>
    public List<ProtocolDisplayItem> BodyItems { get; } = new();

    /// <summary>
    /// Количество строк тела, которые сейчас вставлены в плоский список.
    /// </summary>
    public int VisibleBodyCount { get; set; }

    public bool IsExpanded => HeaderItem.IsExpanded;

    public void AddBodyItem(ProtocolDisplayItem item)
    {
      BodyItems.Add(item);

      if (item.Message.Status == ShowMessageModel.MessageType.Error)
      {
        _hasErrors = true;
      }

      UpdateHeaderBackground();
    }

    public void SetExpanded(bool isExpanded)
    {
      HeaderItem.IsExpanded = isExpanded;
    }

    private void UpdateHeaderBackground()
    {
      HeaderItem.Message.HeaderBackgroundColor = _hasErrors
        ? ErrorBackground
        : SuccessBackground;
    }
  }
}
