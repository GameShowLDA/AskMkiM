using System.Reflection;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Реестр исполнителей команд.
  /// Обеспечивает регистрацию и получение исполнителей
  /// по мнемонике команды.
  /// </summary>
  internal sealed class CommandExecutorRegistry
  {
    /// <summary>
    /// Коллекция зарегистрированных исполнителей команд,
    /// индексируемая по мнемонике команды.
    /// </summary>
    private readonly Dictionary<string, ICommandExecutor> _executors = new(StringComparer.OrdinalIgnoreCase);

    public CommandExecutorRegistry()
    {
      RegisterExecutors();
    }

    /// <summary>
    /// Пытается получить исполнителя команды по её мнемонике.
    /// </summary>
    /// <param name="mnemonic">
    /// Мнемоника команды управляющей программы.
    /// </param>
    /// <param name="executor">
    /// Исполнитель команды, соответствующий указанной мнемонике,
    /// если он найден в реестре.
    /// </param>
    /// <returns>
    /// <c>true</c>, если исполнитель команды найден;
    /// иначе <c>false</c>.
    /// </returns>
    public bool TryGet(string mnemonic, out ICommandExecutor executor) =>
        _executors.TryGetValue(mnemonic, out executor!);

    /// <summary>
    /// Выполняет автоматическое обнаружение и регистрацию
    /// всех исполнителей команд в текущей сборке.
    /// </summary>
    /// <remarks>
    /// Регистрируются все не абстрактные типы,
    /// реализующие интерфейс <see cref="ICommandExecutor"/>.
    /// </remarks>
    private void RegisterExecutors()
    {
      var executorInterface = typeof(ICommandExecutor);

      var types = Assembly.GetExecutingAssembly()
          .GetTypes()
          .Where(t => !t.IsAbstract && executorInterface.IsAssignableFrom(t));

      foreach (var type in types)
      {
        var instance = (ICommandExecutor)Activator.CreateInstance(type)!;
        _executors[instance.Mnemonic] = instance;
      }
    }
  }
}
