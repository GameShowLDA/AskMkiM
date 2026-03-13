using Ask.Core.Shared.Interfaces.EventInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// Содержит события пользовательского интерфейса, связанные с изменением темы оформления.
  /// </summary>
  public class ThemeEvent
  {
    /// <summary>
    /// Событие запроса смены темы интерфейса.
    /// </summary>
    public class Change : IEvent
    {
      /// <summary>
      /// Новая тема, которую необходимо применить.
      /// </summary>
      public ThemeMode NewTheme { get; }

      /// <summary>
      /// Инициализирует событие смены темы интерфейса.
      /// </summary>
      /// <param name="newTheme">Тема, которую требуется применить.</param>
      public Change(ThemeMode newTheme)
      {
        NewTheme = newTheme;
      }
    }

    /// <summary>
    /// Событие, обозначающее, что тема была успешно применена.
    /// </summary>
    public class Changed : IEvent
    {
      /// <summary>
      /// Новая активная тема интерфейса.
      /// </summary>
      public ThemeMode ActiveTheme { get; }

      /// <summary>
      /// Инициализирует событие подтверждения смены темы.
      /// </summary>
      /// <param name="activeTheme">Тема, которая теперь активна.</param>
      public Changed(ThemeMode activeTheme)
      {
        ActiveTheme = activeTheme;
      }
    }

    /// <summary>
    /// Событие, обозначающее, что флаг подствеки была успешно применена.
    /// </summary>
    public class SyntaxHighlighting : IEvent
    {
      /// <summary>
      /// Флаг, отображающий подстветку темы.
      /// </summary>
      public bool IsEnabled { get; }

      /// <summary>
      /// Инициализирует событие подтверждения смены подстветки синтаксиса.
      /// </summary>
      /// <param name="enabled">Флаг подстветки синтаксиса.</param>
      public SyntaxHighlighting(bool enabled)
      {
        IsEnabled = enabled;
      }
    }
  }
}
