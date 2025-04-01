using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Класс-помощник для привязки данных в XAML. Используется для предоставления данных, которые можно привязать, при этом объект может быть заморожен.
  /// </summary>
  /// <remarks>
  /// Этот класс используется в случаях, когда необходимо создать прокси-объект для привязки, особенно когда данные или свойства должны быть доступны в другом контексте, например, в ресурсах.
  /// </remarks>
  public class BindingProxy : Freezable
  {
    /// <summary>
    /// Создает новый экземпляр <see cref="BindingProxy"/>.
    /// </summary>
    /// <returns>Новый экземпляр <see cref="BindingProxy"/>.</returns>
    protected override Freezable CreateInstanceCore()
    {
      return new BindingProxy();
    }

    /// <summary>
    /// Получает или задает данные, которые будут использоваться для привязки в XAML.
    /// </summary>
    /// <value>Объект, который будет привязан.</value>
    public object Data
    {
      get { return (object)GetValue(DataProperty); }
      set { SetValue(DataProperty, value); }
    }

    /// <summary>
    /// Статическое свойство зависимости для хранения данных, которые можно привязать.
    /// </summary>
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
  }
}
