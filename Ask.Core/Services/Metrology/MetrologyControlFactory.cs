using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Windows.Controls;

namespace Ask.Core.Services.Metrology
{
  /// <summary>
  /// Фабрика пользовательских элементов управления метрологических режимов.
  /// </summary>
  /// <remarks>
  /// При создании выполняет рефлексивное сканирование всех загруженных сборок приложения
  /// и находит типы <see cref="UserControl"/>, помеченные атрибутом <see cref="MetrologyModeAttribute"/>.
  /// 
  /// На основе найденных атрибутов формируется сопоставление:
  /// <see cref="MetrologyType"/> → (тип контрола, отображаемое название).
  /// 
  /// Экземпляры контролов создаются через контейнер зависимостей (<see cref="IServiceProvider"/>),
  /// поэтому все зависимости внутри контролов автоматически внедряются (DI).
  /// 
  /// Для добавления нового режима не требуется изменять код фабрики —
  /// достаточно создать <see cref="UserControl"/> и пометить его атрибутом
  /// <see cref="MetrologyModeAttribute"/>.
  /// </remarks>
  public class MetrologyControlFactory
  {
    private readonly IServiceProvider _provider;
    private readonly Dictionary<MetrologyType, (Type type, string title)> _map;

    public MetrologyControlFactory(IServiceProvider provider)
    {
      _provider = provider;

      var assemblies = AppDomain.CurrentDomain.GetAssemblies();

      _map = assemblies
          .SelectMany(a =>
          {
            try { return a.GetTypes(); }
            catch { return Array.Empty<Type>(); }
          })
          .Where(t => !t.IsAbstract && typeof(UserControl).IsAssignableFrom(t))
          .Select(t => (Type: t, Attr: t.GetCustomAttribute<MetrologyModeAttribute>()))
          .Where(x => x.Attr != null)
          .ToDictionary(
              x => x.Attr.Type,
              x => (x.Type, x.Attr.Title));
    }

    /// <summary>
    /// Создаёт пользовательский элемент управления для указанного метрологического режима.
    /// </summary>
    /// <param name="type">Тип метрологического режима.</param>
    /// <returns>
    /// Кортеж, содержащий созданный <see cref="UserControl"/> и заголовок окна режима.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если для указанного режима не найден зарегистрированный контрол.
    /// </exception>
    public (UserControl control, string title) Create(MetrologyType type)
    {
      if (!_map.TryGetValue(type, out var entry))
        throw new InvalidOperationException($"Mode {type} not registered");

      var control = (UserControl)_provider.GetRequiredService(entry.type);
      return (control, entry.title);
    }
  }
}
