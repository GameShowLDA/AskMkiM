using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Reflection;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class CommandFormatManager
  {
    private readonly List<ICommandFormatter> _formatters;

    public CommandFormatManager()
    {
      var iface = typeof(ICommandFormatter);
      _formatters = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => !t.IsAbstract && iface.IsAssignableFrom(t))
        .Select(t => (ICommandFormatter)Activator.CreateInstance(t))
        .ToList();
    }

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      var formatter = _formatters.FirstOrDefault(f => f.CanFormat(model));
      if (formatter != null)
        return formatter.Format(model);

      // Фолбэк: если нет форматтера — выводим просто исходные строки, если есть
      var sourceLinesProp = model.GetType().GetProperty("SourceLines");
      if (sourceLinesProp != null)
        return sourceLinesProp.GetValue(model) as IEnumerable<string> ?? new List<string>();
      return new List<string>();
    }
  }
}
