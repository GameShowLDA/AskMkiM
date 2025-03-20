using System;
using System.Reflection;

namespace NewCore.Function
{
  /// <summary>
  /// Класс для помощи в копировании свойств между объектами.
  /// </summary>
  public class ObjectHelper
  {
    /// <summary>
    /// Копирует свойства из одного объекта в другой. 
    /// Свойства с одинаковыми именами и типами копируются, если свойство в целевом объекте доступно для записи.
    /// </summary>
    /// <param name="source">Объект-источник, из которого будут копироваться свойства.</param>
    /// <param name="target">Объект-цель, в который будут записаны свойства.</param>
    public static void CopyProperties(object source, object target)
    {
      if (source == null || target == null)
      {
        return;
      }

      Type sourceType = source.GetType();
      Type targetType = target.GetType();

      foreach (PropertyInfo sourceProp in sourceType.GetProperties())
      {
        PropertyInfo targetProp = targetType.GetProperty(sourceProp.Name);

        if (targetProp != null && targetProp.CanWrite)
        {
          object value = sourceProp.GetValue(source);
          targetProp.SetValue(target, value);
        }
      }
    }
  }
}
