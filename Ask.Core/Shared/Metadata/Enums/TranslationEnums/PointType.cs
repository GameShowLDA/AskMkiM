namespace Ask.Core.Shared.Metadata.Enums.TranslationEnums
{
  /// <summary>
  /// Тип точки в схеме.
  /// </summary>
  public enum PointType : ushort
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
