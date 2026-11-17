using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ask.UI.Shared.Search;

/// <summary>
/// Подключает поиск к произвольному WPF-компоненту без зависимости от окна поиска.
/// </summary>
public static class SearchTarget
{
  /// <summary>Адаптер поиска компонента.</summary>
  public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
    "Target", typeof(ITextSearchTarget), typeof(SearchTarget), new PropertyMetadata(null));

  /// <summary>Возвращает явно подключённый адаптер.</summary>
  /// <param name="element">Компонент интерфейса.</param>
  /// <returns>Адаптер или отсутствие подключения.</returns>
  public static ITextSearchTarget? GetTarget(DependencyObject element) => (ITextSearchTarget?)element.GetValue(TargetProperty);

  /// <summary>Подключает адаптер к компоненту.</summary>
  /// <param name="element">Компонент интерфейса.</param>
  /// <param name="target">Адаптер поиска.</param>
  public static void SetTarget(DependencyObject element, ITextSearchTarget? target) => element.SetValue(TargetProperty, target);

  /// <summary>Находит подключение на активном компоненте либо его элементе с фокусом.</summary>
  /// <param name="root">Активный компонент рабочей области.</param>
  /// <returns>Явный адаптер, реализация контракта компонентом или его моделью.</returns>
  public static ITextSearchTarget? Resolve(DependencyObject? root)
  {
    if (root == null) return null;
    if (root is Visual visual && Keyboard.FocusedElement is DependencyObject focused
      && focused is Visual && visual.IsAncestorOf(focused))
    {
      for (DependencyObject? item = focused; item != null; item = VisualTreeHelper.GetParent(item))
      {
        var target = ResolveDirect(item);
        if (target != null) return target;
        if (ReferenceEquals(item, root)) break;
      }
    }
    return ResolveDirect(root);
  }

  /// <summary>Проверяет явное подключение, компонент и его модель.</summary>
  /// <param name="element">Компонент интерфейса.</param>
  /// <returns>Первое доступное подключение поиска.</returns>
  private static ITextSearchTarget? ResolveDirect(DependencyObject element) =>
    GetTarget(element) ?? element as ITextSearchTarget ?? (element as FrameworkElement)?.DataContext as ITextSearchTarget;
}
