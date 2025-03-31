using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Представляет элемент стрелки, содержащий имя, описание и геометрические данные для отображения.
  /// </summary>
  public class ArrowItem
  {
    /// <summary>
    /// Получает или задает имя элемента стрелки.
    /// </summary>
    /// <value>Имя элемента стрелки.</value>
    public string Name { get; set; }

    /// <summary>
    /// Получает или задает описание элемента стрелки.
    /// </summary>
    /// <value>Описание элемента стрелки.</value>
    public string Description { get; set; }

    /// <summary>
    /// Получает или задает геометрические данные, которые представляют форму стрелки.
    /// </summary>
    /// <value>Геометрия стрелки, используется для отображения.</value>
    public Geometry GeometryData { get; set; }

    /// <summary>
    /// Возвращает строковое представление объекта <see cref="ArrowItem"/>, которое равно его имени.
    /// </summary>
    /// <returns>Имя элемента стрелки.</returns>
    public override string ToString()
    {
      return Name;
    }
  }
}
