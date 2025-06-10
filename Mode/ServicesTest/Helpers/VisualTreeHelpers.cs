using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace Mode.ServicesTest.Helpers
{
  /// <summary>
  /// Вспомогательные методы для рекурсивного обхода визуального дерева WPF.
  /// </summary>
  public static class VisualTreeHelpers
  {
    /// <summary>
    /// Рекурсивно находит все дочерние элементы заданного типа <typeparamref name="T"/> в визуальном дереве.
    /// </summary>
    /// <typeparam name="T">Тип дочерних элементов, которые необходимо найти.</typeparam>
    /// <param name="depObj">Родительский элемент, от которого начинаем поиск (обычно this или RootContainer).</param>
    /// <returns>Перечисление всех найденных элементов типа T.</returns>
    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj)
        where T : DependencyObject
    {
      // Если элемент null, возвращаем пустую последовательность
      if (depObj == null)
        yield break;

      // Количество дочерних элементов
      int childrenCount = VisualTreeHelper.GetChildrenCount(depObj);
      for (int i = 0; i < childrenCount; i++)
      {
        // Получаем дочерний элемент
        var child = VisualTreeHelper.GetChild(depObj, i);

        // Если он нужного типа, возвращаем его
        if (child is T tChild)
          yield return tChild;

        // Кроме того, рекурсивно обходим детей текущего child
        foreach (T childOfChild in FindVisualChildren<T>(child))
          yield return childOfChild;
      }
    }
  }
}
