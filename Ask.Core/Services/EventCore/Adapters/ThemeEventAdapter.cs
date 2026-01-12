using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Enums.UiEnums;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Статический класс, предоставляющий адаптационный слой для управления темой интерфейса
  /// через событийную систему <see cref="EventCore"/>.
  /// </summary>
  public static class ThemeEventAdapter
  {
    /// <summary>
    /// Публикует событие запроса смены темы интерфейса.
    /// </summary>
    /// <param name="newTheme">Тема, которую необходимо применить.</param>
    public static void RaiseChangeTheme(ThemeMode newTheme) =>
      EventAggregator.Publish(new ThemeEvent.Change(newTheme));

    /// <summary>
    /// Публикует событие подтверждения того, что тема успешно изменилась.
    /// </summary>
    /// <param name="activeTheme">Тема, которая теперь активна.</param>
    public static void RaiseThemeChanged(ThemeMode activeTheme) =>
      EventAggregator.Publish(new ThemeEvent.Changed(activeTheme));

    public static void RaiseSyntaxHighlighting(bool enabled) =>
      EventAggregator.Publish(new ThemeEvent.SyntaxHighlighting(enabled));
  }
}
