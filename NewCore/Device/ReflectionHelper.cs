using System.Reflection;

namespace NewCore.Device
{
  /// <summary>
  /// Класс для поиска классов.
  /// </summary>
  public class ReflectionHelper
  {
    /// <summary>
    /// Получает все классы, реализующие интерфейс <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Тип интерфейса.</typeparam>
    /// <returns>Список типов, которые реализуют интерфейс <typeparamref name="T"/>.</returns>
    public static List<Type> GetAllImplementations<T>()
    {
      return Assembly.GetExecutingAssembly()
          .GetTypes()
          .Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
          .ToList();
    }
  }
}
