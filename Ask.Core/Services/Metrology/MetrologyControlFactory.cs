using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Windows.Controls;

namespace Ask.Core.Services.Metrology
{
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

    public (UserControl control, string title) Create(MetrologyType type)
    {
      if (!_map.TryGetValue(type, out var entry))
        throw new InvalidOperationException($"Mode {type} not registered");

      var control = (UserControl)_provider.GetRequiredService(entry.type);
      return (control, entry.title);
    }
  }
}
