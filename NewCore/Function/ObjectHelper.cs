using System.Reflection;

namespace NewCore.Function
{
  public class ObjectHelper
  {
    public static void CopyProperties(object source, object target)
    {
      if (source == null || target == null) return;

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
