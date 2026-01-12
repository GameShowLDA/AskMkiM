namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  /// <summary>
  /// Унифицированный интерфейс горячей клавиши.
  /// </summary>
  public interface IHotkeyBinding
  {
    string ActionName { get; }
    string KeyCombination { get; }
    bool IsEnabled { get; }
    string? Description { get; }
  }
}
