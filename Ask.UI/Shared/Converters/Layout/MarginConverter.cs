using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Layout
{
  /// <summary>
  /// Конвертер для изменения отступа в зависимости от наличия значения в свойстве Unit.
  /// Если значение Unit не пустое, возвращает отступ в 10 пикселей справа, иначе 0.
  /// </summary>
  public class MarginConverter : IValueConverter
  {
    /// <summary>
    /// Конвертирует значение для задания отступа.
    /// </summary>
    /// <param name="value">Значение, которое нужно конвертировать (в данном случае строка, представляющая Unit).</param>
    /// <param name="targetType">Целевой тип (в данном случае <see cref="Thickness"/>).</param>
    /// <param name="parameter">Параметр для конвертации (не используется в данном случае).</param>
    /// <param name="culture">Культура, используемая для конвертации (не используется в данном случае).</param>
    /// <returns>Возвращает <see cref="Thickness"/> с отступом, основанным на значении Unit.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return string.IsNullOrEmpty(value as string) ? new Thickness(0) : new Thickness(0, 0, 10, 0);
    }

    /// <summary>
    /// Не реализован, так как конвертация назад не требуется.
    /// </summary>
    /// <param name="value">Значение, которое нужно конвертировать обратно.</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Параметр для конвертации (не используется).</param>
    /// <param name="culture">Культура, используемая для конвертации (не используется).</param>
    /// <returns>Метод не реализован, так как не требуется конвертировать обратно.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
