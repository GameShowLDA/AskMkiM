using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Ask.UI.Components.ProtocolListBox
{
  /// <summary>
  /// Внутреннее состояние главной команды протокола.
  /// Хранит заголовок и накопленные строки команды без вложенного визуального дерева.
  /// </summary>
  internal sealed class ProtocolCommandGroup
  {
    private const string SuccessBackgroundResourceKey = "TestsProtocolCommandSuccessBackgroundBrush";
    private const string ErrorBackgroundResourceKey = "TestsProtocolCommandErrorBackgroundBrush";
    private static readonly Color SuccessBackgroundFallback = Color.FromArgb(128, 94, 127, 107);
    private static readonly Color ErrorBackgroundFallback = Color.FromArgb(128, 168, 93, 93);

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
      if (BodyItems.Count == 0)
      {
        HeaderItem.HasChildItems = true;
        HeaderItem.OuterMargin = IsExpanded
          ? new System.Windows.Thickness(0)
          : new System.Windows.Thickness(0, 0, 0, 6);
      }
      else
      {
        var previousLastItem = BodyItems[^1];
        previousLastItem.IsLastGroupItem = false;
        previousLastItem.OuterMargin = new System.Windows.Thickness(0);
      }

      item.IsLastGroupItem = true;
      item.OuterMargin = new System.Windows.Thickness(0, 0, 0, 6);
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
      HeaderItem.OuterMargin = isExpanded && BodyItems.Count > 0
        ? new System.Windows.Thickness(0)
        : new System.Windows.Thickness(0, 0, 0, 6);
    }

    private void UpdateHeaderBackground()
    {
      if (!UserInterfaceConfig.GetSyntaxHighlighting() || !UserInterfaceConfig.GetCommandBodyBackgroundHighlighting())
      {
        HeaderItem.Message.HeaderBackgroundColor = null;
        return;
      }

      HeaderItem.Message.HeaderBackgroundColor = _hasErrors
        ? GetThemeColorOrFallback(ErrorBackgroundResourceKey, ErrorBackgroundFallback)
        : GetThemeColorOrFallback(SuccessBackgroundResourceKey, SuccessBackgroundFallback);
    }

    private static Color GetThemeColorOrFallback(string resourceKey, Color fallbackColor)
    {
      if (Application.Current?.Resources[resourceKey] is SolidColorBrush brush)
      {
        return brush.Color;
      }

      return fallbackColor;
    }
  }
}
