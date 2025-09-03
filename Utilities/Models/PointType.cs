namespace Utilities.Models
{
  public class PointType
  {
    /// <summary>
    /// Тип точки в схеме.
    /// </summary>
    public enum Type : ushort
    {
      /// <summary>
      /// Обычная точка (*)
      /// </summary>
      Star = '*',

      /// <summary>
      /// Точка, разделённая запятой (,)
      /// </summary>
      Comma = ',',

      /// <summary>
      /// Контрольная точка (#)
      /// </summary>
      Hash = '#'
    }
  }
}
